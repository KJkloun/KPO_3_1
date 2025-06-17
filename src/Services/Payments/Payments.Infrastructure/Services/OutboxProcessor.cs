using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Payments.Infrastructure.Data;
using Shared.Infrastructure.MessageBus;
using Shared.Contracts.Messages;

namespace Payments.Infrastructure.Services;

/// <summary>
/// Background service для обработки Outbox сообщений в Payments Service
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payments OutboxProcessor started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payments outbox messages");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessages(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var unprocessedMessages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var message in unprocessedMessages)
        {
            try
            {
                await ProcessMessage(message, messageBus, cancellationToken);
                
                message.ProcessedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Payments outbox message {MessageId} of type {MessageType} processed successfully", 
                    message.Id, message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payments outbox message {MessageId}", message.Id);
                
                message.RetryCount++;
                message.Error = ex.Message;
                
                if (message.RetryCount >= 3)
                {
                    message.ProcessedAt = DateTime.UtcNow; // Mark as processed to avoid infinite retries
                    _logger.LogError("Payments outbox message {MessageId} failed after {RetryCount} retries", 
                        message.Id, message.RetryCount);
                }
                
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task ProcessMessage(Shared.Infrastructure.Outbox.OutboxMessage message, IMessageBus messageBus, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case nameof(PaymentSucceeded):
                var paymentSucceeded = JsonConvert.DeserializeObject<PaymentSucceeded>(message.Content);
                if (paymentSucceeded != null)
                {
                    await messageBus.PublishAsync(paymentSucceeded, cancellationToken);
                }
                break;
                
            case nameof(PaymentFailed):
                var paymentFailed = JsonConvert.DeserializeObject<PaymentFailed>(message.Content);
                if (paymentFailed != null)
                {
                    await messageBus.PublishAsync(paymentFailed, cancellationToken);
                }
                break;
                
            default:
                _logger.LogWarning("Unknown payments message type: {MessageType}", message.Type);
                break;
        }
    }
} 
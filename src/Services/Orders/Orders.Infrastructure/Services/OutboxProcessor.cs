using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Orders.Infrastructure.Data;
using Shared.Infrastructure.MessageBus;
using Shared.Contracts.Messages;
using Orders.Application.Services;

namespace Orders.Infrastructure.Services;

/// <summary>
/// Фоновый сервис для обработки Outbox паттерна
/// Отправляет сообщения из локальной БД в RabbitMQ с гарантией доставки
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5); // Интервал обработки

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            // Ждем перед следующей итерацией
            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Processor stopped");
    }

    /// <summary>
    /// Обрабатывает необработанные сообщения из Outbox
    /// Использует отдельный scope для каждой обработки
    /// </summary>
    private async Task ProcessOutboxMessages()
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Получаем необработанные сообщения
        var messages = await outboxService.GetUnprocessedMessagesAsync();
        
        if (!messages.Any())
            return;

        _logger.LogDebug("Processing {Count} outbox messages", messages.Count());

        foreach (var message in messages)
        {
            try
            {
                // Отправляем в RabbitMQ
                await messageBus.PublishAsync(message.MessageData, message.MessageType);
                
                // Отмечаем как обработанное
                await outboxService.MarkAsProcessedAsync(message.Id);
                
                _logger.LogDebug("Outbox message {MessageId} processed successfully", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                // Сообщение остается необработанным и будет повторно обработано
            }
        }
    }
} 
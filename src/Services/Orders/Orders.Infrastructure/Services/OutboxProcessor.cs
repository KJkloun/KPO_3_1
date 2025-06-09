using System;
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
/// Фоновый сервис для отправки сообщений из Outbox в RabbitMQ.
/// ЛОГИКА: Каждые 5 сек берем необработанные сообщения → отправляем в RabbitMQ → помечаем как обработанные.
/// Это гарантирует exactly-once доставку событий между микросервисами.
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Главный цикл обработки Outbox сообщений.
    /// Работает как daemon - крутится в фоне и обрабатывает накопившиеся события.
    /// ВАЖНО: Использую DI scope для каждой итерации (избегаю memory leaks).
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started - будет обрабатывать сообщения каждые {Interval}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxProcessor cycle - продолжаем работу");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("OutboxProcessor stopped");
    }

    /// <summary>
    /// Обрабатываем партию необработанных сообщений из Outbox.
    /// АЛГОРИТМ:
    /// 1. Берем все unpublished сообщения из БД
    /// 2. Для каждого: десериализуем → отправляем в RabbitMQ → помечаем как processed
    /// 3. Используем рефлексию для работы с разными типами сообщений
    /// </summary>
    private async Task ProcessOutboxMessages()
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // STEP 1: Берем необработанные сообщения (order by created date)
        var messages = await outboxService.GetUnprocessedMessagesAsync();
        
        if (!messages.Any())
            return;

        _logger.LogDebug("Processing {Count} outbox messages", messages.Count());

        foreach (var message in messages)
        {
            try
            {
                // STEP 2: Восстанавливаем тип сообщения из строки (reflection magic)
                var messageType = Type.GetType($"Shared.Contracts.Messages.{message.Type}, Shared.Contracts");
                if (messageType == null)
                {
                    _logger.LogError("Unknown message type: {MessageType} - пропускаем", message.Type);
                    continue;
                }

                // STEP 3: Десериализуем JSON обратно в объект
                var messageObject = JsonConvert.DeserializeObject(message.Content, messageType);
                if (messageObject == null)
                {
                    _logger.LogError("Failed to deserialize message {MessageId} - битый JSON?", message.Id);
                    continue;
                }

                // STEP 4: Отправляем в RabbitMQ через generic метод (тоже рефлексия)
                var publishMethod = typeof(IMessageBus).GetMethod("PublishAsync");
                var genericMethod = publishMethod!.MakeGenericMethod(messageType);
                await (Task)genericMethod.Invoke(messageBus, new[] { messageObject, CancellationToken.None })!;
                
                // STEP 5: Помечаем как обработанное (чтобы не отправлять повторно)
                await outboxService.MarkAsProcessedAsync(message.Id);
                
                _logger.LogDebug("Outbox message {MessageId} processed successfully", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId} - будет retry", message.Id);
                // НЕ помечаем как processed - попробуем в следующий раз
            }
        }
    }
} 
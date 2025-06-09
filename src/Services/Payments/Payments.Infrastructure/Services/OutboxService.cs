using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Payments.Application.Services;
using Payments.Infrastructure.Data;
using Shared.Infrastructure.Outbox;

namespace Payments.Infrastructure.Services;

/// <summary>
/// Реализация Outbox сервиса для Payments
/// </summary>
public class OutboxService : IOutboxService
{
    private readonly PaymentsDbContext _context;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(PaymentsDbContext context, ILogger<OutboxService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveMessageAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(T).Name,
            Content = JsonConvert.SerializeObject(message),
            CreatedAt = DateTime.UtcNow
        };

        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

        _logger.LogInformation("Outbox message {MessageId} of type {MessageType} saved", 
            outboxMessage.Id, typeof(T).Name);
    }
} 
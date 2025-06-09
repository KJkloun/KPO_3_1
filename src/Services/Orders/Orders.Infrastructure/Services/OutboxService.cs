using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Orders.Application.Services;
using Orders.Infrastructure.Data;
using Shared.Infrastructure.Outbox;

namespace Orders.Infrastructure.Services;

/// <summary>
/// Реализация Outbox сервиса для Orders
/// </summary>
public class OutboxService : IOutboxService
{
    private readonly OrdersDbContext _context;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(OrdersDbContext context, ILogger<OutboxService> logger)
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

    public async Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => !m.ProcessedAt.HasValue)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
} 
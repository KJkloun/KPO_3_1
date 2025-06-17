using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Payments.Application.Services;
using Payments.Infrastructure.Data;
using Shared.Infrastructure.Inbox;

namespace Payments.Infrastructure.Services;

/// <summary>
/// Реализация сервиса для работы с Inbox паттерном.
/// Использует EF Core для хранения состояния сообщений.
/// </summary>
public class InboxService : IInboxService
{
    private readonly PaymentsDbContext _dbContext;
    private readonly ILogger<InboxService> _logger;

    public InboxService(PaymentsDbContext dbContext, ILogger<InboxService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsMessageProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InboxMessages
            .AnyAsync(m => m.MessageId == messageId && m.Processed, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveMessageAsync<T>(Guid messageId, T message, CancellationToken cancellationToken = default)
    {
        var inboxMessage = new InboxMessage
        {
            MessageId = messageId,
            MessageType = typeof(T).Name,
            Content = JsonSerializer.Serialize(message),
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.InboxMessages.AddAsync(inboxMessage, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.InboxMessages
            .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

        if (message != null)
        {
            message.Processed = true;
            message.ProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
} 
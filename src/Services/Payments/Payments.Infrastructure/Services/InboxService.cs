using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Payments.Application.Services;
using Payments.Infrastructure.Data;
using Shared.Infrastructure.Inbox;

namespace Payments.Infrastructure.Services;

/// <summary>
/// Реализация Inbox сервиса для Payments (дедупликация сообщений)
/// </summary>
public class InboxService : IInboxService
{
    private readonly PaymentsDbContext _context;
    private readonly ILogger<InboxService> _logger;

    public InboxService(PaymentsDbContext context, ILogger<InboxService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsMessageProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.InboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        return message?.IsProcessed == true;
    }

    public async Task SaveMessageAsync<T>(Guid messageId, T message, CancellationToken cancellationToken = default) where T : class
    {
        var existingMessage = await _context.InboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (existingMessage != null)
        {
            _logger.LogInformation("Inbox message {MessageId} already exists", messageId);
            return;
        }

        var inboxMessage = new InboxMessage
        {
            Id = messageId,
            Type = typeof(T).Name,
            Content = JsonConvert.SerializeObject(message),
            ReceivedAt = DateTime.UtcNow
        };

        await _context.InboxMessages.AddAsync(inboxMessage, cancellationToken);

        _logger.LogInformation("Inbox message {MessageId} of type {MessageType} saved", 
            messageId, typeof(T).Name);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.InboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            _context.InboxMessages.Update(message);

            _logger.LogInformation("Inbox message {MessageId} marked as processed", messageId);
        }
    }
} 
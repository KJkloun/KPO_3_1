namespace Payments.Application.Services;

/// <summary>
/// Интерфейс для работы с Inbox сообщениями (дедупликация)
/// </summary>
public interface IInboxService
{
    Task<bool> IsMessageProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task SaveMessageAsync<T>(Guid messageId, T message, CancellationToken cancellationToken = default) where T : class;
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
} 
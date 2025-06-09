namespace Orders.Application.Services;

/// <summary>
/// Интерфейс для работы с Outbox сообщениями
/// </summary>
public interface IOutboxService
{
    Task SaveMessageAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
} 
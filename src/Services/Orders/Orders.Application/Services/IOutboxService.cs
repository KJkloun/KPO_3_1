using Shared.Infrastructure.Outbox;

namespace Orders.Application.Services;

/// <summary>
/// Интерфейс для работы с Transactional Outbox паттерном.
/// ЦЕЛЬ: Гарантированная доставка событий между микросервисами.
/// ПРИНЦИП: Сохраняем событие в локальную БД (та же транзакция), потом фоновый процесс отправляет в RabbitMQ.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Сохраняет событие в Outbox для последующей отправки.
    /// ВАЖНО: Вызывать в той же транзакции что и основную бизнес-операцию!
    /// </summary>
    /// <typeparam name="T">Тип события (OrderCreated, PaymentSucceeded, etc.)</typeparam>
    /// <param name="message">Объект события для сериализации</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task SaveMessageAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Получает все неотправленные сообщения для обработки.
    /// ИСПОЛЬЗОВАНИЕ: OutboxProcessor берет эти сообщения и отправляет в RabbitMQ.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Коллекция необработанных сообщений, упорядоченных по времени создания</returns>
    Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает сообщение как успешно отправленное.
    /// ВАЖНО: Вызывается только после успешной отправки в RabbitMQ!
    /// </summary>
    /// <param name="messageId">ID сообщения в Outbox</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
} 
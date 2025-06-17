using Shared.Infrastructure.Outbox;

namespace Payments.Application.Services;

/// <summary>
/// Интерфейс для работы с Transactional Outbox паттерном в Payments service.
/// ЦЕЛЬ: Гарантированная доставка событий (PaymentSucceeded/Failed) обратно в Orders.
/// ПРИНЦИП: Сохраняем результат платежа в БД + Outbox, потом фоновый процесс отправляет в RabbitMQ.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Сохраняет событие о результате платежа в Outbox.
    /// ВАЖНО: Вызывать в той же транзакции что и списание денег!
    /// </summary>
    /// <typeparam name="T">Тип события (PaymentSucceeded, PaymentFailed)</typeparam>
    /// <param name="message">Объект события для сериализации</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task SaveMessageAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Получает все неотправленные события о платежах.
    /// ИСПОЛЬЗОВАНИЕ: OutboxProcessor берет эти события и отправляет в RabbitMQ → Orders service.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Коллекция необработанных событий платежей</returns>
    Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает событие о платеже как успешно отправленное.
    /// ВАЖНО: Вызывается только после успешной отправки в RabbitMQ!
    /// </summary>
    /// <param name="messageId">ID события в Outbox</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
} 
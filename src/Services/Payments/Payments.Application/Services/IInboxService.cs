using System;
using System.Threading;
using System.Threading.Tasks;

namespace Payments.Application.Services;

/// <summary>
/// Сервис для работы с Inbox паттерном.
/// Обеспечивает идемпотентность обработки сообщений.
/// </summary>
public interface IInboxService
{
    /// <summary>
    /// Проверяет, было ли сообщение уже обработано.
    /// </summary>
    Task<bool> IsMessageProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет сообщение в Inbox перед обработкой.
    /// </summary>
    Task SaveMessageAsync<T>(Guid messageId, T message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает сообщение как обработанное.
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
} 
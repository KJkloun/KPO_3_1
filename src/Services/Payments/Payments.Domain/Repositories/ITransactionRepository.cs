using System;
using System.Threading;
using System.Threading.Tasks;
using Payments.Domain.Entities;
using Payments.Domain.ValueObjects;

namespace Payments.Domain.Repositories;

/// <summary>
/// Репозиторий для работы с транзакциями.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Добавляет новую транзакцию.
    /// </summary>
    Task<Transaction?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет новую транзакцию.
    /// </summary>
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет все изменения в базе данных.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
} 
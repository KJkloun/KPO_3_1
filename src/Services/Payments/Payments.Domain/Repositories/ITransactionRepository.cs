using Payments.Domain.Entities;

namespace Payments.Domain.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с транзакциями
/// </summary>
public interface ITransactionRepository
{
    Task<Transaction?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
} 
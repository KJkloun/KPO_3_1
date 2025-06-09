using Payments.Domain.Entities;

namespace Payments.Domain.Repositories;

/// <summary>
/// Интерфейс репозитория для работы со счетами
/// </summary>
public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
} 
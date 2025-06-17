using System;
using System.Threading;
using System.Threading.Tasks;
using Payments.Domain.Entities;

namespace Payments.Domain.Repositories;

/// <summary>
/// Репозиторий для работы с аккаунтами пользователей.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Получает аккаунт по идентификатору пользователя.
    /// </summary>
    Task<Account> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет новый аккаунт.
    /// </summary>
    Task AddAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет существующий аккаунт.
    /// </summary>
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет все изменения в базе данных.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
} 
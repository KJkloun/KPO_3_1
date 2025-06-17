namespace Orders.Application.Services;

/// <summary>
/// Сервис для взаимодействия с Payments API
/// </summary>
public interface IPaymentsService
{
    /// <summary>
    /// Проверить баланс пользователя
    /// </summary>
    Task<decimal?> GetUserBalanceAsync(Guid userId, CancellationToken cancellationToken = default);
} 
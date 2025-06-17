namespace Payments.Domain.Enums;

/// <summary>
/// Типы транзакций в системе.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Пополнение баланса
    /// </summary>
    TopUp = 1,

    /// <summary>
    /// Списание средств
    /// </summary>
    Withdrawal = 2
} 
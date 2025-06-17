namespace Payments.Domain.Entities;

/// <summary>
/// Доменная сущность транзакции
/// </summary>
public class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? OrderId { get; private set; } // Null для пополнений
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core нужен parameterless constructor
    private Transaction() { }

    public Transaction(Guid accountId, TransactionType type, decimal amount, string? description = null, Guid? orderId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        Id = Guid.NewGuid();
        AccountId = accountId;
        OrderId = orderId;
        Type = type;
        Amount = amount;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Типы транзакций
/// </summary>
public enum TransactionType
{
    TopUp = 1,
    Withdrawal = 2
} 
namespace Payments.Domain.Entities;

/// <summary>
/// Доменная сущность счета пользователя
/// </summary>
public class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int Version { get; private set; } // Для оптимистичной блокировки

    // EF Core нужен parameterless constructor
    private Account() { }

    public Account(Guid userId)
    {
        Id = userId; // Account ID = User ID для простоты
        UserId = userId;
        Balance = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Version = 1;
    }

    public void TopUp(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public bool TryWithdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        if (Balance < amount)
            return false;

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        return true;
    }
} 
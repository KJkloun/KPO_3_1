namespace Orders.Domain.Entities;

/// <summary>
/// Доменная сущность заказа
/// </summary>
public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core нужен parameterless constructor
    private Order() { }

    public Order(Guid userId, decimal amount, string? description = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        Id = Guid.NewGuid();
        UserId = userId;
        Amount = amount;
        Description = description;
        Status = OrderStatus.Created;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException($"Cannot mark order as paid. Current status: {Status}");

        Status = OrderStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string? reason = null)
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException($"Cannot mark order as failed. Current status: {Status}");

        Status = OrderStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
} 
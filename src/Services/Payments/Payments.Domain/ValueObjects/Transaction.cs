using System;
using Payments.Domain.Enums;

namespace Payments.Domain.ValueObjects;

/// <summary>
/// Транзакция - запись об операции с балансом аккаунта.
/// </summary>
public class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; }
    public Guid? OrderId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Transaction() { }

    public Transaction(
        Guid accountId,
        TransactionType type,
        decimal amount,
        string description,
        Guid? orderId = null)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Type = type;
        Amount = amount;
        Description = description;
        OrderId = orderId;
        CreatedAt = DateTime.UtcNow;
    }
} 
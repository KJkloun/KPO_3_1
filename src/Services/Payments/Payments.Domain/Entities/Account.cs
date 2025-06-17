using System;
using System.Collections.Generic;
using Payments.Domain.ValueObjects;

namespace Payments.Domain.Entities;

/// <summary>
/// Аккаунт пользователя для хранения средств.
/// Реализует бизнес-логику операций с балансом.
/// </summary>
public class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private Account() { }

    public Account(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Balance = 0;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Пополняет баланс аккаунта.
    /// </summary>
    /// <param name="amount">Сумма пополнения</param>
    /// <returns>True если операция успешна</returns>
    public bool TryTopUp(decimal amount)
    {
        if (amount <= 0)
            return false;

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Списывает средства с аккаунта.
    /// </summary>
    /// <param name="amount">Сумма списания</param>
    /// <returns>True если операция успешна</returns>
    public bool TryWithdraw(decimal amount)
    {
        if (amount <= 0 || amount > Balance)
            return false;

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Добавляет транзакцию в историю операций.
    /// </summary>
    public void AddTransaction(Transaction transaction)
    {
        _transactions.Add(transaction);
    }
} 
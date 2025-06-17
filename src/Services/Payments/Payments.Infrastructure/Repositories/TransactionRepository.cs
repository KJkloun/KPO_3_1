using Microsoft.EntityFrameworkCore;
using Payments.Domain.Entities;
using Payments.Domain.Repositories;
using Payments.Infrastructure.Data;

namespace Payments.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы с транзакциями
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly PaymentsDbContext _context;

    public TransactionRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
} 
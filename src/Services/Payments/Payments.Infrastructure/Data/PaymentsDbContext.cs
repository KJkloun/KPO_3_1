using Microsoft.EntityFrameworkCore;
using Payments.Domain.Entities;
using Payments.Domain.ValueObjects;
using Shared.Infrastructure.Inbox;
using Shared.Infrastructure.Outbox;

namespace Payments.Infrastructure.Data;

/// <summary>
/// Контекст базы данных для сервиса платежей.
/// </summary>
public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Конфигурация Account
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Balance).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.Version).IsRequired().IsConcurrencyToken();

            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // Конфигурация Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountId).IsRequired();
            entity.Property(e => e.OrderId);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.OrderId).IsUnique().HasFilter("[OrderId] IS NOT NULL");
        });

        // Конфигурация InboxMessage
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ReceivedAt).IsRequired();
            entity.Property(e => e.ProcessedAt);
            entity.Property(e => e.Error);

            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => e.ReceivedAt);
        });

        // Конфигурация OutboxMessage
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ProcessedAt);
            entity.Property(e => e.Error);
            entity.Property(e => e.RetryCount).HasDefaultValue(0);

            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
} 
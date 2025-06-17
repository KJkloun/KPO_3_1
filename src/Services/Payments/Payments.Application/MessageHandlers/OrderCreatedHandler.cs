using MediatR;
using Microsoft.Extensions.Logging;
using Payments.Application.Services;
using Payments.Domain.Repositories;
using Payments.Domain.Entities;
using Shared.Contracts.Messages;

namespace Payments.Application.MessageHandlers;

/// <summary>
/// Обработчик события OrderCreated из RabbitMQ.
/// Реализует семантику exactly-once через Transactional Inbox.
/// </summary>
public class OrderCreatedHandler
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IInboxService _inboxService;
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IInboxService inboxService,
        ILogger<OrderCreatedHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _inboxService = inboxService;
        _logger = logger;
    }

    /// <summary>
    /// Обрабатывает событие создания заказа:
    /// 1. Проверяет идемпотентность через Inbox
    /// 2. Находит аккаунт пользователя
    /// 3. Списывает средства
    /// 4. Сохраняет транзакцию
    /// </summary>
    public async Task Handle(OrderCreated message, CancellationToken cancellationToken = default)
    {
        var messageId = Guid.NewGuid();
        
        try
        {
            if (await _inboxService.IsMessageProcessedAsync(messageId, cancellationToken))
            {
                _logger.LogInformation("Message {MessageId} already processed, skipping", messageId);
                return;
            }

            await _inboxService.SaveMessageAsync(messageId, message, cancellationToken);

            var account = await _accountRepository.GetByUserIdAsync(message.UserId, cancellationToken);
            if (account == null)
            {
                _logger.LogError("Account not found for user {UserId} when processing order {OrderId}",
                    message.UserId, message.OrderId);
                return;
            }

            var withdrawalSuccess = account.TryWithdraw(message.Amount);
            if (!withdrawalSuccess)
            {
                _logger.LogWarning("Withdrawal failed for order {OrderId}: Insufficient balance. Current balance: {Balance}, Required: {Amount}",
                    message.OrderId, account.Balance, message.Amount);
                return;
            }

            var transaction = new Transaction(
                account.Id,
                TransactionType.Withdrawal,
                message.Amount,
                $"Payment for order {message.OrderId}",
                message.OrderId
            );

            await _accountRepository.UpdateAsync(account, cancellationToken);
            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _inboxService.MarkAsProcessedAsync(messageId, cancellationToken);
            
            await _accountRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} processed successfully. Amount {Amount} withdrawn from account {AccountId}",
                message.OrderId, message.Amount, account.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process order {OrderId} for user {UserId}",
                message.OrderId, message.UserId);
            throw;
        }
    }
} 
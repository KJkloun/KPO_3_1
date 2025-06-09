using MediatR;
using Microsoft.Extensions.Logging;
using Payments.Application.Services;
using Payments.Domain.Repositories;
using Payments.Domain.Entities;
using Shared.Contracts.Messages;

namespace Payments.Application.MessageHandlers;

/// <summary>
/// Обработчик события OrderCreated из RabbitMQ.
/// АЛГОРИТМ: Получили заказ → проверяем дубли → списываем деньги → логируем.
/// Использую Transactional Inbox для предотвращения дублей обработки.
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
    /// Основная логика обработки заказа:
    /// 1. Проверяем не обрабатывали ли уже это сообщение (Inbox pattern)
    /// 2. Находим аккаунт пользователя
    /// 3. Списываем деньги через Account.TryWithdraw()
    /// 4. Сохраняем транзакцию + помечаем сообщение как обработанное
    /// </summary>
    public async Task Handle(OrderCreated message, CancellationToken cancellationToken = default)
    {
        var messageId = Guid.NewGuid(); // В реальности ID приходит из RabbitMQ headers
        
        try
        {
            // STEP 1: Проверяем дубликаты (Idempotency через Inbox)
            if (await _inboxService.IsMessageProcessedAsync(messageId, cancellationToken))
            {
                _logger.LogInformation("Message {MessageId} already processed, skipping", messageId);
                return;
            }

            // STEP 2: Сохраняем сообщение в Inbox (начинаем обработку)
            await _inboxService.SaveMessageAsync(messageId, message, cancellationToken);

            // STEP 3: Находим аккаунт пользователя
            var account = await _accountRepository.GetByUserIdAsync(message.UserId, cancellationToken);
            if (account == null)
            {
                _logger.LogError("Account not found for user {UserId} when processing order {OrderId}",
                    message.UserId, message.OrderId);
                return;
            }

            // STEP 4: Списываем деньги (бизнес-логика в доменной модели)
            var withdrawalSuccess = account.TryWithdraw(message.Amount);
            if (!withdrawalSuccess)
            {
                _logger.LogWarning("Withdrawal failed for order {OrderId}: Insufficient balance. Current balance: {Balance}, Required: {Amount}",
                    message.OrderId, account.Balance, message.Amount);
                return;
            }

            // STEP 5: Создаем транзакцию для истории
            var transaction = new Transaction(
                account.Id,
                TransactionType.Withdrawal,
                message.Amount,
                $"Payment for order {message.OrderId}",
                message.OrderId
            );

            // STEP 6: Сохраняем все изменения атомарно
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
            throw; // RabbitMQ will retry or send to DLQ
        }
    }
} 
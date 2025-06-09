using MediatR;
using Microsoft.Extensions.Logging;
using Payments.Application.Commands;
using Payments.Domain.Entities;
using Payments.Domain.Repositories;
using Shared.Contracts.Messages;

namespace Payments.Application.MessageHandlers;

/// <summary>
/// Обработчик события создания заказа из RabbitMQ
/// Списывает деньги с аккаунта пользователя
/// </summary>
public class OrderCreatedHandler
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IInboxService _inboxService;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<OrderCreatedHandler> _logger;
    private readonly IMediator _mediator;

    public OrderCreatedHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IInboxService inboxService,
        IOutboxService outboxService,
        ILogger<OrderCreatedHandler> logger,
        IMediator mediator)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _inboxService = inboxService;
        _outboxService = outboxService;
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Обработка события создания заказа
    /// Вызывается асинхронно через RabbitMQ
    /// </summary>
    public async Task Handle(OrderCreated orderCreated)
    {
        try
        {
            _logger.LogInformation("Processing order created event for order {OrderId}, user {UserId}, amount {Amount}",
                orderCreated.OrderId, orderCreated.UserId, orderCreated.Amount);

            // Проверяем дедупликацию через Inbox
            if (await _inboxService.IsMessageProcessedAsync(orderCreated.OrderId))
            {
                _logger.LogInformation("Order {OrderId} already processed, skipping", orderCreated.OrderId);
                return;
            }

            // Отмечаем сообщение как полученное
            await _inboxService.SaveMessageAsync(orderCreated.OrderId, orderCreated);

            // Проверяем, что транзакция еще не была создана (дополнительная защита)
            var existingTransaction = await _transactionRepository.GetByOrderIdAsync(orderCreated.OrderId);
            if (existingTransaction != null)
            {
                _logger.LogWarning("Transaction for order {OrderId} already exists", orderCreated.OrderId);
                await _inboxService.MarkAsProcessedAsync(orderCreated.OrderId);
                return;
            }

            // Получаем счет пользователя
            var account = await _accountRepository.GetByUserIdAsync(orderCreated.UserId);
            if (account == null)
            {
                await _outboxService.SaveMessageAsync(new PaymentFailed
                {
                    OrderId = orderCreated.OrderId,
                    UserId = orderCreated.UserId,
                    Amount = orderCreated.Amount,
                    ProcessedAt = DateTime.UtcNow,
                    Reason = "Account not found"
                });

                await _inboxService.MarkAsProcessedAsync(orderCreated.OrderId);
                await _accountRepository.SaveChangesAsync();
                return;
            }

            // Списываем деньги с аккаунта (Account ID = User ID)
            var command = new ProcessPaymentCommand
            {
                OrderId = orderCreated.OrderId,
                AccountId = orderCreated.UserId, // Упрощение: Account ID = User ID
                Amount = orderCreated.Amount,
                Description = $"Payment for order {orderCreated.OrderId}"
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // Создаем транзакцию
                var transaction = new Transaction(
                    account.Id,
                    TransactionType.Withdrawal,
                    orderCreated.Amount,
                    $"Payment for order {orderCreated.OrderId}",
                    orderCreated.OrderId);

                await _transactionRepository.AddAsync(transaction);
                await _accountRepository.UpdateAsync(account);

                // Отправляем сообщение об успешном платеже
                await _outboxService.SaveMessageAsync(new PaymentSucceeded
                {
                    OrderId = orderCreated.OrderId,
                    UserId = orderCreated.UserId,
                    Amount = orderCreated.Amount,
                    ProcessedAt = DateTime.UtcNow,
                    TransactionId = transaction.Id
                });

                await _inboxService.MarkAsProcessedAsync(orderCreated.OrderId);
                await _accountRepository.SaveChangesAsync();

                _logger.LogInformation("Payment processed successfully for order {OrderId}", orderCreated.OrderId);
            }
            else
            {
                _logger.LogWarning("Payment failed for order {OrderId}: {Error}", 
                    orderCreated.OrderId, result.ErrorMessage);

                await _outboxService.SaveMessageAsync(new PaymentFailed
                {
                    OrderId = orderCreated.OrderId,
                    UserId = orderCreated.UserId,
                    Amount = orderCreated.Amount,
                    ProcessedAt = DateTime.UtcNow,
                    Reason = result.ErrorMessage
                });

                await _inboxService.MarkAsProcessedAsync(orderCreated.OrderId);
                await _accountRepository.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order created event for order {OrderId}", orderCreated.OrderId);
            throw; // Пусть RabbitMQ обработает ошибку (retry/dead letter queue)
        }
    }
} 
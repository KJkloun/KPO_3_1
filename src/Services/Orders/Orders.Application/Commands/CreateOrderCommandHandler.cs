using MediatR;
using Microsoft.Extensions.Logging;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;
using Orders.Application.Services;
using Shared.Contracts.Messages;

namespace Orders.Application.Commands;

/// <summary>
/// Обработчик создания заказа с проверкой баланса
/// Реализует паттерн CQRS и Transactional Outbox
/// </summary>
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOutboxService _outboxService;
    private readonly IPaymentsService _paymentsService; // HTTP клиент к Payments API
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IOutboxService outboxService,
        IPaymentsService paymentsService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _outboxService = outboxService;
        _paymentsService = paymentsService;
        _logger = logger;
    }

    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Шаг 1: Проверяем баланс пользователя через Payments API
            var userBalance = await _paymentsService.GetUserBalanceAsync(request.UserId, cancellationToken);
            
            if (userBalance == null)
            {
                _logger.LogWarning("Could not retrieve balance for user {UserId}", request.UserId);
                return new CreateOrderResult
                {
                    OrderId = Guid.Empty,
                    IsSuccess = false,
                    ErrorMessage = "Could not verify account balance. Please ensure you have a payment account."
                };
            }

            // Шаг 2: Проверяем достаточность средств
            if (userBalance < request.Amount)
            {
                _logger.LogWarning("Insufficient balance for user {UserId}. Balance: {Balance}, Required: {Amount}", 
                    request.UserId, userBalance, request.Amount);
                return new CreateOrderResult
                {
                    OrderId = Guid.Empty,
                    IsSuccess = false,
                    ErrorMessage = $"Insufficient balance. Current balance: {userBalance:C}, Required: {request.Amount:C}"
                };
            }

            // Шаг 3: Создаем заказ в Orders DB
            var order = new Order(request.UserId, request.Amount, request.Description);
            await _orderRepository.AddAsync(order, cancellationToken);

            // Шаг 4: Сохраняем событие в Outbox для асинхронной обработки
            // Это гарантирует exactly-once доставку сообщения в RabbitMQ
            var orderCreatedMessage = new OrderCreated
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = order.Amount,
                CreatedAt = order.CreatedAt,
                Description = order.Description
            };

            await _outboxService.SaveMessageAsync(orderCreatedMessage, cancellationToken);

            // Шаг 5: Одна транзакция для заказа + outbox сообщения
            await _orderRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} created successfully for user {UserId}. Amount: {Amount}", 
                order.Id, order.UserId, order.Amount);

            return new CreateOrderResult
            {
                OrderId = order.Id,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for user {UserId}", request.UserId);

            return new CreateOrderResult
            {
                OrderId = Guid.Empty,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
} 
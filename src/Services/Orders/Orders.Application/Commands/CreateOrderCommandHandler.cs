using MediatR;
using Microsoft.Extensions.Logging;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;
using Orders.Application.Services;
using Shared.Contracts.Messages;

namespace Orders.Application.Commands;

/// <summary>
/// Основной обработчик создания заказов в системе.
/// ВАЖНО: Сначала проверяем баланс через HTTP к Payments, только потом создаем заказ.
/// Использую Transactional Outbox для надежной доставки событий в RabbitMQ.
/// </summary>
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOutboxService _outboxService;
    private readonly IPaymentsService _paymentsService;
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

    /// <summary>
    /// Основная логика создания заказа:
    /// 1. Проверяем баланс пользователя в Payments service (HTTP вызов)
    /// 2. Создаем заказ в локальной БД Orders
    /// 3. Сохраняем событие OrderCreated в Outbox для асинхронной отправки
    /// 4. OutboxProcessor потом отправит событие в RabbitMQ → Payments service спишет деньги
    /// </summary>
    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // STEP 1: Валидируем баланс через HTTP API (синхронно, для UX)
            var userBalance = await _paymentsService.GetUserBalanceAsync(request.UserId, cancellationToken);
            
            if (userBalance == null)
            {
                _logger.LogWarning("User {UserId} not found in Payments service", request.UserId);
                return new CreateOrderResult
                {
                    OrderId = Guid.Empty,
                    IsSuccess = false,
                    ErrorMessage = "Account not found. Please create a payment account first."
                };
            }

            if (userBalance < request.Amount)
            {
                _logger.LogWarning("Insufficient funds for user {UserId}. Balance: {Balance}, Required: {Amount}", 
                    request.UserId, userBalance, request.Amount);
                return new CreateOrderResult
                {
                    OrderId = Guid.Empty,
                    IsSuccess = false,
                    ErrorMessage = $"Insufficient balance. Current: {userBalance:C}, Required: {request.Amount:C}"
                };
            }

            // STEP 2: Создаем заказ (локальная транзакция)
            var order = new Order(request.UserId, request.Amount, request.Description);
            await _orderRepository.AddAsync(order, cancellationToken);

            // STEP 3: Сохраняем событие в Outbox (та же транзакция что и заказ)
            // КРИТИЧНО: Outbox и Order сохраняются атомарно!
            var orderCreatedMessage = new OrderCreated
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = order.Amount,
                CreatedAt = order.CreatedAt,
                Description = order.Description
            };

            await _outboxService.SaveMessageAsync(orderCreatedMessage, cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", 
                order.Id, order.UserId);

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
                ErrorMessage = "Internal server error. Please try again."
            };
        }
    }
} 
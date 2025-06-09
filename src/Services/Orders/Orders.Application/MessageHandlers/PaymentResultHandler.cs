using Microsoft.Extensions.Logging;
using Orders.Domain.Repositories;
using Shared.Contracts.Messages;

namespace Orders.Application.MessageHandlers;

/// <summary>
/// Обработчик сообщений о результатах платежей
/// </summary>
public class PaymentResultHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PaymentResultHandler> _logger;

    public PaymentResultHandler(
        IOrderRepository orderRepository,
        ILogger<PaymentResultHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task HandlePaymentSucceeded(PaymentSucceeded message)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(message.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for payment success", message.OrderId);
                return;
            }

            order.MarkAsPaid();
            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} marked as paid", message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment success for order {OrderId}", message.OrderId);
            throw;
        }
    }

    public async Task HandlePaymentFailed(PaymentFailed message)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(message.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for payment failure", message.OrderId);
                return;
            }

            order.MarkAsFailed(message.Reason);
            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} marked as failed: {Reason}", message.OrderId, message.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment failure for order {OrderId}", message.OrderId);
            throw;
        }
    }
} 
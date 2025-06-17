using MediatR;

namespace Orders.Application.Queries;

/// <summary>
/// Запрос для получения заказа по ID
/// </summary>
public record GetOrderByIdQuery : IRequest<OrderDto?>
{
    public Guid OrderId { get; init; }
} 
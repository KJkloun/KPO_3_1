using MediatR;
using Orders.Domain.Repositories;

namespace Orders.Application.Queries;

/// <summary>
/// Обработчик запроса для получения списка заказов
/// </summary>
public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, IEnumerable<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return orders.Select(order => new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Amount = order.Amount,
            Status = order.Status,
            Description = order.Description,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        });
    }
} 
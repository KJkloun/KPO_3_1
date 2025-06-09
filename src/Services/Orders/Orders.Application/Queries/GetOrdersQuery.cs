using MediatR;
using Orders.Domain.Entities;

namespace Orders.Application.Queries;

/// <summary>
/// Запрос для получения списка заказов пользователя
/// </summary>
public record GetOrdersQuery : IRequest<IEnumerable<OrderDto>>
{
    public Guid UserId { get; init; }
}

/// <summary>
/// DTO для заказа
/// </summary>
public record OrderDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public OrderStatus Status { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
} 
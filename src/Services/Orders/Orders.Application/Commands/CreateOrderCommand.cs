using MediatR;

namespace Orders.Application.Commands;

/// <summary>
/// Команда для создания заказа
/// </summary>
public record CreateOrderCommand : IRequest<CreateOrderResult>
{
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Результат создания заказа
/// </summary>
public record CreateOrderResult
{
    public Guid OrderId { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
} 
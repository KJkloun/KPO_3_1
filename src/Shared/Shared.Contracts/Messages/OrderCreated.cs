namespace Shared.Contracts.Messages;

/// <summary>
/// Сообщение о создании заказа
/// </summary>
public record OrderCreated
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Description { get; init; }
} 
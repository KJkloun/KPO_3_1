namespace Orders.Domain.Entities;

/// <summary>
/// Статусы заказа
/// </summary>
public enum OrderStatus
{
    Created = 1,
    Paid = 2,
    Failed = 3
} 
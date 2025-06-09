namespace Shared.Contracts.Messages;

/// <summary>
/// Сообщение об успешном платеже
/// </summary>
public record PaymentSucceeded
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public DateTime ProcessedAt { get; init; }
    public Guid TransactionId { get; init; }
}

/// <summary>
/// Сообщение о неудачном платеже
/// </summary>
public record PaymentFailed
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public DateTime ProcessedAt { get; init; }
    public string Reason { get; init; } = string.Empty;
} 
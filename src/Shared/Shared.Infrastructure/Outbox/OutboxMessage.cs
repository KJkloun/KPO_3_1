namespace Shared.Infrastructure.Outbox;

/// <summary>
/// Сущность для паттерна Transactional Outbox
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public bool IsProcessed => ProcessedAt.HasValue;
} 
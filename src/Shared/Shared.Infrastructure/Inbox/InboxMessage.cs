namespace Shared.Infrastructure.Inbox;

/// <summary>
/// Сущность для паттерна Transactional Inbox (дедупликация сообщений)
/// </summary>
public class InboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public bool IsProcessed => ProcessedAt.HasValue;
} 
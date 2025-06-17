using System;

namespace Payments.Infrastructure.Data;

/// <summary>
/// Сущность для хранения входящих сообщений в Inbox.
/// Используется для обеспечения идемпотентности.
/// </summary>
public class InboxMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string MessageType { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
} 
namespace NotificationService.Domain;

public class Notification
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}

public enum NotificationType
{
    OrderPlaced = 0,
    OrderConfirmed = 1,
    OrderCancelled = 2,
    OrderFailed = 3
}

public enum NotificationChannel
{
    Email = 0,
    SMS = 1,
    Push = 2
}

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}
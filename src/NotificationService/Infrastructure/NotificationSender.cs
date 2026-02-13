using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public interface INotificationSender
{
    Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default);
}

public class MockNotificationSender : INotificationSender
{
    private readonly ILogger<MockNotificationSender> _logger;

    public MockNotificationSender(ILogger<MockNotificationSender> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // Simulate sending delay
        await Task.Delay(500, cancellationToken);

        // Log the notification (in real system, would send email/SMS/push)
        _logger.LogInformation(
            "📧 Notification sent via {Channel} to customer {CustomerId} | Type: {Type} | Subject: {Subject}",
            notification.Channel, notification.CustomerId, notification.Type, notification.Subject);

        return true;
    }
}
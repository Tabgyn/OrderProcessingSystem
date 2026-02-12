namespace SharedKernel.Events;

public class PaymentProcessed : BaseEvent
{
    public override string EventType => nameof(PaymentProcessed);

    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

public class PaymentFailed : BaseEvent
{
    public override string EventType => nameof(PaymentFailed);

    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}

public class PaymentRefunded : BaseEvent
{
    public override string EventType => nameof(PaymentRefunded);

    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; }
}
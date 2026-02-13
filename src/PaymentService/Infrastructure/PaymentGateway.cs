namespace PaymentService.Infrastructure;

public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(decimal amount, string paymentMethod, CancellationToken cancellationToken = default);
}

public class MockPaymentGateway : IPaymentGateway
{
    private readonly ILogger<MockPaymentGateway> _logger;
    private static readonly Random _random = new();

    public MockPaymentGateway(ILogger<MockPaymentGateway> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, string paymentMethod, CancellationToken cancellationToken = default)
    {
        // Simulate payment processing delay
        await Task.Delay(1000, cancellationToken);

        // Simulate 90% success rate
        var success = _random.Next(100) < 90;

        if (success)
        {
            var transactionId = $"TXN-{Guid.NewGuid():N}";
            _logger.LogInformation("Payment processed successfully. Transaction ID: {TransactionId}", transactionId);

            return new PaymentResult
            {
                IsSuccess = true,
                TransactionId = transactionId
            };
        }
        else
        {
            _logger.LogWarning("Payment processing failed");

            return new PaymentResult
            {
                IsSuccess = false,
                ErrorCode = "INSUFFICIENT_FUNDS",
                ErrorMessage = "Payment declined by gateway"
            };
        }
    }
}

public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
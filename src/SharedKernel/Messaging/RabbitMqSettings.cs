namespace SharedKernel.Messaging;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException("RabbitMQ Host is required");

        if (Port <= 0)
            throw new InvalidOperationException("RabbitMQ Port must be greater than 0");

        if (string.IsNullOrWhiteSpace(Username))
            throw new InvalidOperationException("RabbitMQ Username is required");

        if (string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("RabbitMQ Password is required");
    }
}
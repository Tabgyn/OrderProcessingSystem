using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Commands;
using OrderService.Data;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests;

public class OrderServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public OrderServiceIntegrationTests()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("orderprocessing_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    // Replace DB with test container
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<OrderDbContext>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));

                    // Replace RabbitMQ settings
                    var rabbitDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(SharedKernel.Messaging.RabbitMqSettings));
                    if (rabbitDescriptor != null)
                        services.Remove(rabbitDescriptor);

                    services.AddSingleton(new SharedKernel.Messaging.RabbitMqSettings
                    {
                        Host = _rabbitMqContainer.Hostname,
                        Port = _rabbitMqContainer.GetMappedPublicPort(5672),
                        Username = "guest",
                        Password = "guest",
                        VirtualHost = "/"
                    });
                });
            });

        _client = _factory.CreateClient();

        // Apply migrations
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }

    [Fact]
    public async Task PlaceOrder_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var command = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            Items: new List<PlaceOrderItem>
            {
                new(Guid.NewGuid(), "Laptop", 1, 999.99m)
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PlaceOrderResult>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.OrderId);
        Assert.Equal(999.99m, result.TotalAmount);
    }

    [Fact]
    public async Task PlaceOrder_EmptyItems_ReturnsBadRequest()
    {
        // Arrange
        var command = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            Items: new List<PlaceOrderItem>()
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_ExistingOrder_ReturnsOrder()
    {
        // Arrange - Create an order first
        var command = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            Items: new List<PlaceOrderItem>
            {
                new(Guid.NewGuid(), "Mouse", 2, 29.99m)
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/orders", command);
        var created = await createResponse.Content.ReadFromJsonAsync<PlaceOrderResult>();

        // Act
        var getResponse = await _client.GetAsync($"/api/orders/{created!.OrderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var order = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(created.OrderId, order.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetOrder_NonExistingOrder_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomerOrders_ExistingCustomer_ReturnsTwoOrders()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        for (int i = 0; i < 2; i++)
        {
            var command = new PlaceOrderCommand(
                CustomerId: customerId,
                Items: new List<PlaceOrderItem>
                {
                    new(Guid.NewGuid(), "Keyboard", 1, 79.99m)
                }
            );
            await _client.PostAsJsonAsync("/api/orders", command);
        }

        // Act
        var response = await _client.GetAsync($"/api/orders/customer/{customerId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orders = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(orders);
        Assert.Equal(2, orders.Length);
    }
}
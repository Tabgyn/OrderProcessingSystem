using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderService.Application.Commands;
using OrderService.Application.Queries;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Infrastructure;
using RabbitMQ.Client;
using Scalar.AspNetCore;
using SharedKernel.Extensions;
using SharedKernel.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Database
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDatabase")));

// Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IEventStoreRepository, EventStoreRepository>();

// Command and Query Handlers
builder.Services.AddScoped<IPlaceOrderCommandHandler, PlaceOrderCommandHandler>();
builder.Services.AddScoped<IGetOrderQueryHandler, GetOrderQueryHandler>();
builder.Services.AddScoped<IGetCustomerOrdersQueryHandler, GetCustomerOrdersQueryHandler>();

// Event Bus
builder.Services.AddScoped<IEventBus, RabbitMqEventBus>();

// RabbitMQ Configuration
var rabbitMqSettings = builder.Configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>();
rabbitMqSettings?.Validate();
builder.Services.AddSingleton(rabbitMqSettings!);

// Add after RabbitMQ configuration
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("OrderDatabase")!,
        name: "postgresql",
        tags: new[] { "db" })
    .AddCheck("rabbitmq", () =>
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = rabbitMqSettings!.Host,
                Port = rabbitMqSettings.Port,
                UserName = rabbitMqSettings.Username,
                Password = rabbitMqSettings.Password,
                VirtualHost = rabbitMqSettings.VirtualHost
            };
            using var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            return HealthCheckResult.Healthy("RabbitMQ connection successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection failed", ex);
        }
    }, tags: new[] { "messaging" });

// Event Bus
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

// Event Consumers
builder.Services.AddHostedService<InventoryReservedConsumer>();
builder.Services.AddHostedService<InventoryReservationFailedConsumer>();
builder.Services.AddHostedService<PaymentProcessedConsumer>();
builder.Services.AddHostedService<PaymentFailedConsumer>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.MapHealthCheckEndpoints();

// Apply migrations on startup (development only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Apply migrations
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
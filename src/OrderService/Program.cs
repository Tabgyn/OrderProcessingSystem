using Microsoft.EntityFrameworkCore;
using OrderService.Application.Commands;
using OrderService.Application.Queries;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Infrastructure;
using SharedKernel.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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

// Event Bus
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

// Event Consumers
builder.Services.AddHostedService<InventoryReservedConsumer>();
builder.Services.AddHostedService<InventoryReservationFailedConsumer>();
builder.Services.AddHostedService<PaymentProcessedConsumer>();
builder.Services.AddHostedService<PaymentFailedConsumer>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Apply migrations on startup (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
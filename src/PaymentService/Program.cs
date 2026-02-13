using Microsoft.EntityFrameworkCore;
using PaymentService.Consumers;
using PaymentService.Data;
using PaymentService.Infrastructure;
using Scalar.AspNetCore;
using SharedKernel.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDatabase")));

// Repository
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Payment Gateway
builder.Services.AddSingleton<IPaymentGateway, MockPaymentGateway>();

// RabbitMQ Configuration
var rabbitMqSettings = builder.Configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>();
rabbitMqSettings?.Validate();
builder.Services.AddSingleton(rabbitMqSettings!);

// Event Bus
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

// Event Consumers
builder.Services.AddHostedService<InventoryReservedConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Apply migrations
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
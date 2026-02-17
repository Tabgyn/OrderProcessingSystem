using Microsoft.EntityFrameworkCore;
using InventoryService.Consumers;
using InventoryService.Data;
using InventoryService.Infrastructure;
using SharedKernel.Messaging;
using Scalar.AspNetCore;
using SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Database
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("InventoryDatabase")));

// Repository
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

// RabbitMQ Configuration
var rabbitMqSettings = builder.Configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>();
rabbitMqSettings?.Validate();
builder.Services.AddSingleton(rabbitMqSettings!);

builder.Services.AddHealthChecks()
    .AddNpgSql(
        name: "postgresql",
        tags: new[] { "db" })
    .AddRabbitMQ(
        name: "rabbitmq",
        tags: new[] { "messaging" });

// Event Bus
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

// Event Consumers
builder.Services.AddHostedService<OrderPlacedConsumer>();
builder.Services.AddHostedService<InventoryReleasedConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Apply migrations and seed data
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseAuthorization();
app.MapControllers();

app.MapHealthCheckEndpoints();

app.Run();
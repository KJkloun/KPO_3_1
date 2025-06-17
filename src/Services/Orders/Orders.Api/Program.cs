using Microsoft.EntityFrameworkCore;
using Orders.Application.Commands;
using Orders.Application.MessageHandlers;
using Orders.Application.Services;
using Orders.Domain.Repositories;
using Orders.Infrastructure.Data;
using Orders.Infrastructure.Repositories;
using Orders.Infrastructure.Services;
using Shared.Contracts.Messages;
using Shared.Infrastructure.MessageBus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString == "InMemory")
{
    builder.Services.AddDbContext<OrdersDbContext>(options =>
        options.UseInMemoryDatabase("OrdersDb"));
}
else
{
    builder.Services.AddDbContext<OrdersDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly));

// Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Services
builder.Services.AddScoped<IOutboxService, OutboxService>();

// HTTP Client для Payments API
builder.Services.AddHttpClient<IPaymentsService, PaymentsService>(client =>
{
    var paymentsBaseUrl = builder.Configuration.GetValue<string>("PaymentsApi:BaseUrl") ?? "http://localhost:5002";
    client.BaseAddress = new Uri(paymentsBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Message Bus (только если не InMemory)
if (connectionString != "InMemory")
{
    builder.Services.AddSingleton<IMessageBus>(provider =>
    {
        var logger = provider.GetRequiredService<ILogger<RabbitMqMessageBus>>();
        var rabbitConnectionString = builder.Configuration.GetConnectionString("RabbitMQ") ?? "amqp://localhost";
        return new RabbitMqMessageBus(logger, rabbitConnectionString);
    });

    // Message Handlers
    builder.Services.AddScoped<PaymentResultHandler>();
    
    // Outbox Processor
    builder.Services.AddHostedService<OutboxProcessor>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS before Authorization
app.UseCors();

app.UseAuthorization();
app.MapControllers();

// Ensure database is created and migrated
if (connectionString != "InMemory")
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        context.Database.EnsureCreated();
    }

    // Subscribe to payment result messages
    Task.Run(async () =>
    {
        await Task.Delay(3000); // Даем время для инициализации системы
        
        using var scope = app.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await messageBus.SubscribeAsync<PaymentSucceeded>(async (message) =>
        {
            using var handlerScope = app.Services.CreateScope();
            var paymentHandler = handlerScope.ServiceProvider.GetRequiredService<PaymentResultHandler>();
            await paymentHandler.HandlePaymentSucceeded(message);
        });
        
        await messageBus.SubscribeAsync<PaymentFailed>(async (message) =>
        {
            using var handlerScope = app.Services.CreateScope();
            var paymentHandler = handlerScope.ServiceProvider.GetRequiredService<PaymentResultHandler>();
            await paymentHandler.HandlePaymentFailed(message);
        });
    });
}
else
{
    // Ensure database is created for InMemory
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    context.Database.EnsureCreated();
}

app.Run(); 
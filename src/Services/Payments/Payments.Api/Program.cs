using Microsoft.EntityFrameworkCore;
using Payments.Application.Commands;
using Payments.Application.MessageHandlers;
using Payments.Application.Services;
using Payments.Domain.Repositories;
using Payments.Infrastructure.Data;
using Payments.Infrastructure.Repositories;
using Payments.Infrastructure.Services;
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
    builder.Services.AddDbContext<PaymentsDbContext>(options =>
        options.UseInMemoryDatabase("PaymentsDb"));
}
else
{
    builder.Services.AddDbContext<PaymentsDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateAccountCommand).Assembly));

// Repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// Services
builder.Services.AddScoped<IInboxService, InboxService>();
builder.Services.AddScoped<IOutboxService, OutboxService>();

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
    builder.Services.AddScoped<OrderCreatedHandler>();
    
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
        var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        context.Database.EnsureCreated();
    }

    // Subscribe to order created messages
    Task.Run(async () =>
    {
        await Task.Delay(3000); // Даем время для инициализации системы
        
        using var scope = app.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        
        await messageBus.SubscribeAsync<OrderCreated>(async (message) =>
        {
            using var handlerScope = app.Services.CreateScope();
            var orderHandler = handlerScope.ServiceProvider.GetRequiredService<OrderCreatedHandler>();
            await orderHandler.HandleOrderCreated(message);
        });
    });
}
else
{
    // Ensure database is created for InMemory
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    context.Database.EnsureCreated();
}

app.Run(); 
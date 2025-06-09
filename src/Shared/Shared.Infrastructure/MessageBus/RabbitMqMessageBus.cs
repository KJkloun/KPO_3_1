using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Shared.Infrastructure.MessageBus;

/// <summary>
/// Реализация шины сообщений через RabbitMQ
/// </summary>
public class RabbitMqMessageBus : IMessageBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqMessageBus> _logger;

    public RabbitMqMessageBus(ILogger<RabbitMqMessageBus> logger, string connectionString = "amqp://localhost")
    {
        _logger = logger;
        
        var factory = new ConnectionFactory()
        {
            Uri = new Uri(connectionString),
            DispatchConsumersAsync = true
        };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var queueName = typeof(T).Name;
        
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var json = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published message of type {MessageType} to queue {QueueName}", 
            typeof(T).Name, queueName);
        
        await Task.CompletedTask;
    }

    public async Task SubscribeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class
    {
        var queueName = typeof(T).Name;
        
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonConvert.DeserializeObject<T>(json);

                if (message != null)
                {
                    await handler(message);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    
                    _logger.LogInformation("Successfully processed message of type {MessageType}", typeof(T).Name);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize message of type {MessageType}", typeof(T).Name);
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message of type {MessageType}", typeof(T).Name);
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        
        _logger.LogInformation("Started consuming messages of type {MessageType} from queue {QueueName}", 
            typeof(T).Name, queueName);
            
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
} 
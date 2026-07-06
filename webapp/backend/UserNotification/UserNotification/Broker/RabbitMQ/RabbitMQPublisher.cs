using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace UserNotification.Broker.RabbitMQ
{
    public class RabbitMQPublisher : IRabbitMQPublisher
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMQPublisher> _logger;

        public RabbitMQPublisher(IConnectionFactory connectionFactory, ILogger<RabbitMQPublisher> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task PublishAsync<T>(string queueName, T message)
        {
            try
            {
                using (var connection = await _connectionFactory.CreateConnectionAsync())
                using (var channel = await connection.CreateChannelAsync())
                {
                    await channel.QueueDeclareAsync(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    var json = JsonSerializer.Serialize(message);
                    var body = Encoding.UTF8.GetBytes(json);

                    var properties = new BasicProperties
                    {
                        Persistent = true,
                        ContentType = "application/json"
                    };

                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: queueName,
                        mandatory: false,
                        basicProperties: properties,
                        body: body);

                    _logger.LogInformation("Message published to queue '{QueueName}'", queueName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to queue '{QueueName}'", queueName);
                throw;
            }
        }

        public async Task PublishAsync<T>(string exchangeName, string routingKey, T message)
        {
            try
            {
                using (var connection = await _connectionFactory.CreateConnectionAsync())
                using (var channel = await connection.CreateChannelAsync())
                {
                    await channel.ExchangeDeclareAsync(
                        exchange: exchangeName,
                        type: ExchangeType.Topic,
                        durable: true,
                        autoDelete: false,
                        arguments: null);

                    var json = JsonSerializer.Serialize(message);
                    var body = Encoding.UTF8.GetBytes(json);

                    var properties = new BasicProperties
                    {
                        Persistent = true,
                        ContentType = "application/json"
                    };

                    await channel.BasicPublishAsync(
                        exchange: exchangeName,
                        routingKey: routingKey,
                        mandatory: false,
                        basicProperties: properties,
                        body: body);

                    _logger.LogInformation("Message published to exchange '{ExchangeName}' with routing key '{RoutingKey}'", exchangeName, routingKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to exchange '{ExchangeName}'", exchangeName);
                throw;
            }
        }
    }
}

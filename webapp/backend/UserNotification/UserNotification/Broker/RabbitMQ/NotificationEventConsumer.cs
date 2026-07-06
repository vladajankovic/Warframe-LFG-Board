
using GroupRequests.Broker.Events;
using Microsoft.OpenApi;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Audit.Broker.RabbitMQ
{
    public class NotificationEventConsumer : IBrokerEventConsumer
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<NotificationEventConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public NotificationEventConsumer(IConnectionFactory connectionFactory, ILogger<NotificationEventConsumer> logger, IServiceProvider serviceProvider)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task StartConsuming(CancellationToken cancellationToken)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                const string queueName = "group-request-created";

                // Declare queue to ensure it exists
                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);

                // Set QoS to process one message at a time
                await channel.BasicQosAsync(0, 1, false, cancellationToken);

                _logger.LogInformation("Starting to consume messages from queue: {QueueName}", queueName);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        var groupRequestEvent = JsonSerializer.Deserialize<GroupRequestCreatedEvent>(message);

                        if (groupRequestEvent != null)
                        {
                            await ProcessGroupRequestEvent(groupRequestEvent);
                            await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                            _logger.LogInformation("Processed group request event: {UserId} - {ItemName} by {InGameName}",
                                groupRequestEvent.UserId, groupRequestEvent.ItemName, groupRequestEvent.InGameName);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize group request event message");
                            await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing group request event from queue");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: false,
                    consumerTag: "notification-consumer",
                    noLocal: false,
                    exclusive: false,
                    arguments: null,
                    consumer: consumer,
                    cancellationToken: cancellationToken);

                // Keep consuming until cancellation is requested
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Group request event consumer stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in group request event consumer");
                throw;
            }
        }

        private async Task ProcessGroupRequestEvent(GroupRequestCreatedEvent auditEvent)
        {
            
        }
    }
}

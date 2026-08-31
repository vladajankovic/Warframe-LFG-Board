using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using GroupRequests.Broker.Events;
using GroupRequests.Models;
using GroupRequests.Repositories;
using GroupRequests.Services;

namespace GroupRequests.Broker.RabbitMQ
{
    public class GroupRequestsEventConsumer : IBrokerEventConsumer
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<GroupRequestsEventConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public GroupRequestsEventConsumer(
            IConnectionFactory connectionFactory,
            ILogger<GroupRequestsEventConsumer> logger,
            IServiceProvider serviceProvider)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task StartConsuming(CancellationToken cancellationToken)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionWithRetryAsync(_logger, cancellationToken);

                using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                const string queueName = "directory-user-deleted-gr";

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

                        var usersDirectoryEvent = JsonSerializer.Deserialize<UserDeletedEventMessage>(message);

                        if (usersDirectoryEvent != null)
                        {
                            await ProcessUsersDirectoryEvent(usersDirectoryEvent);
                            await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                            _logger.LogInformation("Processed users directory event: Delete user {UserId}", usersDirectoryEvent.UserId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize users directory event message");
                            await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing users directory event from queue");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: false,
                    consumerTag: "grouprequest-consumer",
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
                _logger.LogInformation("Event consumer stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event consumer");
                throw;
            }
        }

        private async Task ProcessUsersDirectoryEvent(UserDeletedEventMessage userDeletedEvent)
        {
            using var scope = _serviceProvider.CreateScope();
            var groupRequestsRepository = scope.ServiceProvider.GetRequiredService<IGroupRequestsRepository>();

            int count = await groupRequestsRepository.DeleteAllGroupRequestForUser(userDeletedEvent.UserId);

            if (count > 0)
            {
                _logger.LogInformation("Deleted {count} group requests for user {userId}.", count, userDeletedEvent.UserId);
            }
            else
            {
                _logger.LogInformation("No group requests deleted for user {userId}.", userDeletedEvent.UserId);
            }
        }
    }
}

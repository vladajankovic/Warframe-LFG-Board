
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using UserNotification.Broker.Events;
using UserNotification.Hubs;
using UserNotification.Models;
using UserNotification.Repositories;
using UserNotification.Services;

namespace UserNotification.Broker.RabbitMQ
{
    public class NotificationEventConsumer : IBrokerEventConsumer
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<NotificationEventConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<UserNotificationHub> _hubContext;

        public NotificationEventConsumer(
            IConnectionFactory connectionFactory,
            ILogger<NotificationEventConsumer> logger,
            IServiceProvider serviceProvider,
            IHubContext<UserNotificationHub> hubContext)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        public async Task StartConsuming(CancellationToken cancellationToken)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionWithRetryAsync(_logger, cancellationToken);

                var consumers = new[]
                {
                    CreateChannelForGroupRequests(connection, cancellationToken),
                    CreateChannelForUsersDirectory(connection, cancellationToken),
                };

                await Task.WhenAll(consumers);
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

        private async Task CreateChannelForGroupRequests(IConnection connection, CancellationToken cancellationToken)
        {
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

        private async Task CreateChannelForUsersDirectory(IConnection connection, CancellationToken cancellationToken)
        {
            using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            const string queueName = "directory-user-deleted";

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
                consumerTag: "notification-consumer",
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer,
                cancellationToken: cancellationToken);

            // Keep consuming until cancellation is requested
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        private async Task ProcessGroupRequestEvent(GroupRequestCreatedEvent groupRequestEvent)
        {
            using var scope = _serviceProvider.CreateScope();
            var favoritesRepository = scope.ServiceProvider.GetRequiredService<IUserFavoritesRepository>();
            var notificationsService = scope.ServiceProvider.GetRequiredService<IUserNotificationsService>();

            var persistedNotification = await notificationsService.Create(
                groupRequestEvent.UserId,
                groupRequestEvent.ItemName,
                groupRequestEvent.InGameName,
                groupRequestEvent.CreatedAt);

            var userIds = await favoritesRepository.GetSubscribedUserIds(groupRequestEvent.ItemName);

            if (userIds.Count == 0)
            {
                _logger.LogInformation(
                    "Stored notification {NotificationId}, but no connected subscribed users were found for item {ItemName}",
                    persistedNotification.Id,
                    groupRequestEvent.ItemName);
                return;
            }

            var notification = new GroupRequestNotification
            {
                Id = persistedNotification.Id,
                GroupRequestUserId = persistedNotification.GroupRequestUserId,
                ItemName = persistedNotification.ItemName,
                InGameName = persistedNotification.InGameName,
                CreatedAt = persistedNotification.CreatedAt
            };

            var sendTasks = userIds.Select(userId =>
                _hubContext
                    .Clients
                    .Group(UserNotificationHub.GetUserGroupName(userId))
                    .SendAsync(UserNotificationHub.GroupRequestCreatedMethod, notification));

            await Task.WhenAll(sendTasks);

            _logger.LogInformation(
                "Sent group request notification for item {ItemName} to {RecipientCount} subscribed users",
                groupRequestEvent.ItemName,
                userIds.Count);
        }

        private async Task ProcessUsersDirectoryEvent(UserDeletedEventMessage userDeletedEvent)
        {
            using var scope = _serviceProvider.CreateScope();
            var favoritesRepository = scope.ServiceProvider.GetRequiredService<IUserFavoritesRepository>();

            int count = await favoritesRepository.DeleteAllFromUser(userDeletedEvent.UserId);

            if (count > 0)
            {
                _logger.LogInformation("Deleted {count} favorites for user {userId}.", count, userDeletedEvent.UserId);
            }
            else
            {
                _logger.LogInformation("No favorites deleted for user {userId}.", userDeletedEvent.UserId);
            }
        }
    }
}

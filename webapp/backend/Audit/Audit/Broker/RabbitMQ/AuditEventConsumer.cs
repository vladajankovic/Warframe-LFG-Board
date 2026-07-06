using Audit.Commands;
using Audit.Repository;
using Microsoft.OpenApi;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Audit.Broker.RabbitMQ
{
    public class AuditEventConsumer : IBrokerEventConsumer
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<AuditEventConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public AuditEventConsumer(IConnectionFactory connectionFactory, ILogger<AuditEventConsumer> logger, IServiceProvider serviceProvider)
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
                const string queueName = "audit";

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

                        var auditEvent = JsonSerializer.Deserialize<AuditEventMessage>(message);

                        if (auditEvent != null)
                        {
                            await ProcessAuditEvent(auditEvent);
                            await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                            _logger.LogInformation("Processed audit event: {Service} - {Action} by {User}",
                                auditEvent.Service, auditEvent.Action, auditEvent.User);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize audit event message");
                            await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing audit event from queue");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: false,
                    consumerTag: "audit-consumer",
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
                _logger.LogInformation("Audit event consumer stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audit event consumer");
                throw;
            }
        }

        private async Task ProcessAuditEvent(AuditEventMessage auditEvent)
        {
            using var scope = _serviceProvider.CreateScope();
            var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditRepository>();

            var command = new CreateAuditLogCommand
            {
                DateTime = auditEvent.DateTime,
                Service = auditEvent.Service,
                Action = auditEvent.Action,
                User = auditEvent.User,
                Details = auditEvent.Details
            };

            await auditRepository.CreateAuditLog(command);
        }
    }
}

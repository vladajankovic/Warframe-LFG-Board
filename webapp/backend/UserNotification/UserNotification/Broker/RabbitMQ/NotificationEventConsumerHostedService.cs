namespace UserNotification.Broker.RabbitMQ
{
    public class NotificationEventConsumerHostedService : BackgroundService
    {
        private readonly IBrokerEventConsumer _consumer;
        private readonly ILogger<NotificationEventConsumerHostedService> _logger;

        public NotificationEventConsumerHostedService(IBrokerEventConsumer consumer, ILogger<NotificationEventConsumerHostedService> logger)
        {
            _consumer = consumer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Norification Event Consumer Hosted Service is starting");

            try
            {
                await _consumer.StartConsuming(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Notification Event Consumer Hosted Service");
                throw;
            }

            _logger.LogInformation("Notification Event Consumer Hosted Service has stopped");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Notification Event Consumer Hosted Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}

namespace GroupRequests.Broker.RabbitMQ
{
    public class GroupRequestsEventConsumerHostedService : BackgroundService
    {
        private readonly IBrokerEventConsumer _consumer;
        private readonly ILogger<GroupRequestsEventConsumerHostedService> _logger;

        public GroupRequestsEventConsumerHostedService(IBrokerEventConsumer consumer, ILogger<GroupRequestsEventConsumerHostedService> logger)
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
                _logger.LogError(ex, "Fatal error in GroupRequests Event Consumer Hosted Service");
                throw;
            }

            _logger.LogInformation("GroupRequests Event Consumer Hosted Service has stopped");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GroupRequests Event Consumer Hosted Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}

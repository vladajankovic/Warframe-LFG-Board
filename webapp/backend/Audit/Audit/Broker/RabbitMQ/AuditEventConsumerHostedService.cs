namespace Audit.Broker.RabbitMQ
{
    public class AuditEventConsumerHostedService : BackgroundService
    {
        private readonly IAuditEventConsumer _consumer;
        private readonly ILogger<AuditEventConsumerHostedService> _logger;

        public AuditEventConsumerHostedService(IAuditEventConsumer consumer, ILogger<AuditEventConsumerHostedService> logger)
        {
            _consumer = consumer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Audit Event Consumer Hosted Service is starting");

            try
            {
                await _consumer.StartConsuming(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Audit Event Consumer Hosted Service");
                throw;
            }

            _logger.LogInformation("Audit Event Consumer Hosted Service has stopped");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Audit Event Consumer Hosted Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}

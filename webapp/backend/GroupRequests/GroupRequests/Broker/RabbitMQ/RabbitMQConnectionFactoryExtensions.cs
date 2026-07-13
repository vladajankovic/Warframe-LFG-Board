using RabbitMQ.Client;

namespace GroupRequests.Broker.RabbitMQ
{
    internal static class RabbitMQConnectionFactoryExtensions
    {
        private const int MaxRetryCount = 5;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        public static async Task<IConnection> CreateConnectionWithRetryAsync(
            this IConnectionFactory connectionFactory,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            for (var retryCount = 0; ; retryCount++)
            {
                try
                {
                    return await connectionFactory.CreateConnectionAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (retryCount < MaxRetryCount)
                {
                    var retryAttempt = retryCount + 1;

                    logger.LogWarning(
                        ex,
                        "RabbitMQ connection failed. Retry {RetryAttempt} of {MaxRetryCount} in {RetryDelaySeconds} seconds",
                        retryAttempt,
                        MaxRetryCount,
                        RetryDelay.TotalSeconds);

                    await Task.Delay(RetryDelay, cancellationToken);
                }
            }
        }
    }
}

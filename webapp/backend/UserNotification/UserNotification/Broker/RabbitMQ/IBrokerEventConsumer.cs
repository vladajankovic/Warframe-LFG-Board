namespace Audit.Broker.RabbitMQ
{
    public interface IBrokerEventConsumer
    {
        Task StartConsuming(CancellationToken cancellationToken);
    }
}

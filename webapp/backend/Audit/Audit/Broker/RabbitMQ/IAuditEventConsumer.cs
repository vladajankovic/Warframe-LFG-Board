namespace Audit.Broker.RabbitMQ
{
    public interface IAuditEventConsumer
    {
        Task StartConsuming(CancellationToken cancellationToken);
    }
}

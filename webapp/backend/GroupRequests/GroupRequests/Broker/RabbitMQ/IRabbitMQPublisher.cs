namespace GroupRequests.Broker.RabbitMQ
{
    public interface IRabbitMQPublisher
    {
        Task PublishAsync<T>(string queueName, T message);
        Task PublishAsync<T>(string exchangeName, string routingKey, T message);
    }
}

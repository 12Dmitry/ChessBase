namespace ChessBase.Kafka;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(
        string topic, 
        TMessage message, 
        CancellationToken cancellationToken = default) where TMessage : class;
}
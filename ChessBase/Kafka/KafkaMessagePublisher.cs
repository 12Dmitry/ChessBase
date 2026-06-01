using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChessBase.Kafka;

public sealed class KafkaMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaMessagePublisher> _logger;

    public KafkaMessagePublisher(IOptions<KafkaOptions> options, ILogger<KafkaMessagePublisher> logger)
    {
        _logger = logger;

        // Инициализация нативного клиента librdkafka происходит ОДИН раз за жизнь приложения
        _producer = new ProducerBuilder<string, string>(options.Value.Producer)
            .SetErrorHandler((_, error) => 
                _logger.LogError("Kafka internal error: {Reason}, Fatal: {IsFatal}", error.Reason, error.IsFatal))
            .Build();
    }

    public async Task PublishAsync<TMessage>(string topic, TMessage message,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        try
        {
            var payload = JsonSerializer.Serialize(message);

            var kafkaMessage = new Message<string, string>
            {
                Value = payload
            };

            // ProduceAsync возвращает Task, который завершается ТОЛЬКО после получения Delivery Report от брокера
            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

            _logger.LogDebug("Message delivered to partition {Partition} with offset {Offset}", 
                deliveryResult.Partition, deliveryResult.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to deliver message to Kafka topic {Topic}. Error: {Error}", 
                topic, ex.Error.Reason);
            
            throw; 
        }
    }

    public void Dispose()
    {
        try
        {
            _logger.LogInformation("Shutting down Kafka producer, flushing internal buffers...");
            
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while disposing Kafka producer");
        }
    }
}
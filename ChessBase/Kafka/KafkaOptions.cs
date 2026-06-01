using Confluent.Kafka;

namespace ChessBase.Kafka;

public class KafkaOptions
{
    public string BootstrapServers { get; set; }
    public string TopicGamesToAnalyze { get; set; }

    // Вложенные объекты для Confluent конфигураций
    public ProducerConfig Producer { get; set; } = new();
    public ConsumerConfig Consumer { get; set; } = new();
}
using Confluent.Kafka;
using static Confluent.Kafka.ConfigPropertyNames;

namespace KafkaLibrary
{
	public interface IKafkaProduceService: IDisposable
	{
		Task PublishAsync<T>(string topic, T message);
	}
}

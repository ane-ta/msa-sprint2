using Confluent.Kafka;

namespace KafkaLibrary
{

	public delegate Task StringMessageReceivedHandler(Null key, string value);

	public interface IKafkaConsumeService
	{
		event StringMessageReceivedHandler OnMessageReceived;
		void StartConsume(string topicName, CancellationToken cancellationToken);
	}
}

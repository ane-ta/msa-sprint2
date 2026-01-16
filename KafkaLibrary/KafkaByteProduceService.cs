using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace KafkaLibrary
{
	//public class KafkaByteProduceService : IKafkaProduceService
	//{
	//	private IProducer<Null, byte[]> _producer;
	//	private ILogger<KafkaByteProduceService> _logger;

	//	public KafkaByteProduceService(ProducerConfig config, ILogger<KafkaByteProduceService> logger)
	//	{
	//		_logger = logger;
	//		_producer = new ProducerBuilder<Null, byte[]>(config).Build();
	//	}

	//	public void Dispose()
	//	{
	//		_producer.Flush();
	//		_producer.Dispose();
	//	}
	//	public async Task PublishAsync<T>(string topic, T message)
	//	{
	//		byte[] byteArray;
	//		using (var stream = new System.IO.MemoryStream())
	//		{
	//			message.WriteTo(stream);
	//			byteArray = stream.ToArray();
	//		}

	//		try
	//		{
	//			var dr = await _producer.ProduceAsync(topic, new Message<Null, byte[]> { Value = byteArray });
	//			_logger.LogInformation($"Delivered message to {dr.TopicPartitionOffset}");
	//		}
	//		catch (ProduceException<Null, byte[]> e)
	//		{
	//			_logger.LogError($"Delivery for message {message.Id} failed: {e.Error.Reason}");
	//			throw;
	//		}
	//	}
	//}
}

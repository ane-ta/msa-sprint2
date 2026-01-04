using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KafkaLibrary
{
	public class KafkaStringProduceService : IKafkaProduceService
	{
		private IProducer<Null, string> _producer;
		private ILogger<KafkaStringProduceService> _logger;

		public KafkaStringProduceService( ProducerConfig config, ILogger<KafkaStringProduceService> logger)
		{
			_logger = logger;
			_producer = new ProducerBuilder<Null, string>(config).Build();
		}

		public void Dispose()
		{
			_producer.Flush();
			_producer.Dispose();
		}

		public async Task PublishAsync<T>(string topic, T message)
		{
			var str = JsonSerializer.Serialize(message);

			try
			{
				var dr = await _producer.ProduceAsync(topic, new Message<Null, string> { Value = str });
				_logger.LogInformation($"Delivered message to '{dr.TopicPartitionOffset}'");
			}
			catch (ProduceException<Null, string> e)
			{
				_logger.LogError($"Delivery to {topic} failed: {e.Error.Reason}");
				throw;
			}
		}
	}
}

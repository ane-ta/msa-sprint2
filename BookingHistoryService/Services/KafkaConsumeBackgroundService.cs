using KafkaLibrary;

namespace BookingHistoryService.Services
{
	public class KafkaConsumeBackgroundService : BackgroundService
	{
		private readonly IKafkaConsumeService _consumerService;
		private readonly string _topicName;

		public KafkaConsumeBackgroundService(IKafkaConsumeService consumerService, IConfiguration config)
		{
			_consumerService = consumerService;
			_topicName = config["Kafka:TopicName"]!;

			ArgumentException.ThrowIfNullOrWhiteSpace(_topicName, "Kafka:TopicName");
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			return Task.Run(() =>
			{
				_consumerService.StartConsume(_topicName, stoppingToken);
			}, stoppingToken);
		}
	}
}

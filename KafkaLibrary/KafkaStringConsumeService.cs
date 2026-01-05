using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace KafkaLibrary
{
	// Класс потребителя
	public class KafkaStringConsumeService : IKafkaConsumeService, IDisposable
	{
		private readonly IConsumer<Null, string> _consumer;
		private readonly ILogger<KafkaStringConsumeService> _logger;

		public event StringMessageReceivedHandler? OnMessageReceived;

		public KafkaStringConsumeService(ConsumerConfig config, ILogger<KafkaStringConsumeService> logger)
		{
			_logger = logger;
			
			config.EnableAutoCommit = false;
			
			var consumerBuilder = new ConsumerBuilder<Null, string>(config);

			_consumer = consumerBuilder.Build();
		}

		// Этот метод запускает бесконечный цикл чтения
		public void StartConsume(string topicName, CancellationToken cancellationToken)
		{
			_consumer.Subscribe(topicName);
			_logger.LogInformation($"Consumer subscribed to topic: {topicName}");

			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						// Опрашиваем Kafka на предмет новых сообщений (таймаут 100 мс)
						var consumeResult = _consumer.Consume(cancellationToken);

						ProcessMessage(consumeResult).Wait(cancellationToken);
					}
					catch (ConsumeException e)
					{
						_logger.LogError(e, $"Error consuming message: {e.Error.Reason}");
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Это ожидаемое исключение при завершении работы приложения
				_logger.LogInformation("Consumer loop cancelled.");
			}
			finally
			{
				// Важно: Отписка и закрытие соединения при выходе из цикла
				_consumer.Close();
			}
		}

		private async Task ProcessMessage(ConsumeResult<Null, string> result)
		{
			_logger.LogInformation($"Received message: Key='{result.Message.Key}', Value='{result.Message.Value}', Offset='{result.Offset}'");

			var ev = OnMessageReceived;
			if ( ev == null)
			{
				_logger.LogWarning("Message received but no subscribers to the event!");
				_consumer.Commit(result); // Можно коммитить сразу, если некому обрабатывать
				return;
			}

			try
			{
				await ev.Invoke(result.Message.Key, result.Message.Value);

				_consumer.Commit(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during message handling by event subscribers. Offset not committed.");
			}
		}

		public void Dispose()
		{
			_consumer.Dispose();
		}
	}
}

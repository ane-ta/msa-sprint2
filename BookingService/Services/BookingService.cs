using Booking;
using Confluent.Kafka;
using Google.Protobuf;
using Grpc.Core;

namespace BookingService.Services;

public class BookingService : Booking.BookingService.BookingServiceBase
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<BookingService> _logger;

	public BookingService(IConfiguration configuration, ILogger<BookingService> logger)
	{
		_configuration = configuration;
		_logger = logger;
	}

	// --- Реализация gRPC метода CreateBooking ---
	public override async Task<BookingResponse> CreateBooking(BookingRequest request, ServerCallContext context)
	{
		_logger.LogInformation($"Received booking request for user {request.UserId} hotel {request.HotelId}");

		// В реальном приложении здесь была бы логика бизнес-валидации,
		// взаимодействие с базой данных и расчет цены/скидки.

		// Генерируем ID и текущее время
		var bookingId = Guid.NewGuid().ToString();
		var creationTime = DateTime.UtcNow;

		// Подготовка сообщения для Kafka (можно использовать BookingResponse как формат события)
		var bookingEvent = new BookingResponse
		{
			Id = bookingId,
			UserId = request.UserId,
			HotelId = request.HotelId,
			PromoCode = request.PromoCode ?? "",
			DiscountPercent = 10.0, // Пример расчета
			Price = 150.00,       // Пример расчета
			CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(creationTime).ToString() // Используем ISO-8601 строку
		};

		// Отправка сообщения в Kafka асинхронно
		await ProduceKafkaMessageAsync("hotel-booking-events", bookingEvent);

		// Возвращаем ответ gRPC клиенту
		return bookingEvent;
	}

	// --- Реализация gRPC метода ListBookings (заглушка) ---
	public override Task<BookingListResponse> ListBookings(BookingListRequest request, ServerCallContext context)
	{
		// Здесь должна быть логика обращения к БД для получения списка бронирований пользователя
		_logger.LogInformation($"Received list request for user {request.UserId}");

		// Возвращаем пустой список как заглушку
		var response = new BookingListResponse();
		// response.Bookings.Add(...); // Здесь можно добавить реальные данные

		return Task.FromResult(response);
	}

	// --- Метод-помощник для отправки в Kafka ---
	private async Task ProduceKafkaMessageAsync(string topic, BookingResponse message)
	{
		var config = new ProducerConfig { BootstrapServers = _configuration["Kafka:BootstrapServers"] };

		using (var producer = new ProducerBuilder<Null, ByteString>(config).Build())
		{
			// Сериализация Protobuf сообщения в байты
			var messageBytes = message.ToByteString();
			var dr = await producer.ProduceAsync(topic, new Message<Null, ByteString> { Value = messageBytes });
			_logger.LogInformation($"Delivered message to {dr.TopicPartitionOffset}");
		}
	}
}

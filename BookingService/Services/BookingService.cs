using BookingMicroService.Grpc;
using BookingService.Models.MonolothDtos;
using Confluent.Kafka;
using Google.Protobuf;
using Grpc.Core;
using System.Text.Json;

namespace BookingService.Services;

public class BookingService : BookingMicroService.Grpc.BookingService.BookingServiceBase
{
	private readonly IConfiguration _configuration;
	private readonly IHttpClientFactory _clientFactory;
	private readonly ILogger<BookingService> _logger;

	public BookingService(IConfiguration configuration, ILogger<BookingService> logger, IHttpClientFactory clientFactory)
	{
		_configuration = configuration;
		_logger = logger;
		_clientFactory = clientFactory;
	}

	// --- Реализация gRPC метода CreateBooking ---
	public override async Task<BookingResponse> CreateBooking(BookingRequest request, ServerCallContext context)
	{
		_logger.LogInformation($"Received booking request for user {request.UserId} hotel {request.HotelId}");

		// В реальном приложении здесь была бы логика бизнес-валидации,
		// взаимодействие с базой данных и расчет цены/скидки.
		await ValidateUser(request.UserId);


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

	private async Task ValidateUser(string userId)
	{
		var httpClient = _clientFactory.CreateClient();

		var url = $"http://monolith:8080/api/users/{userId}";

		var response = await httpClient.GetAsync(url);

		if (response.IsSuccessStatusCode)
		{
			var dataJson = await response.Content.ReadAsStringAsync();
			var userDto = JsonSerializer.Deserialize<User>(dataJson);

			if(userDto == null)
			{
				_logger.LogError("Empty data for userId");
				throw new RpcException(new Status(StatusCode.Internal, $"Invalid data from monolith API for user ID"));
			}
			if (!userDto.active)
			{
				_logger.LogWarning($"User {userId} is inactive");
				throw new RpcException(new Status(StatusCode.FailedPrecondition, $"User is inactive"));
			}
			
			if ( userDto.blacklisted )
			{
				_logger.LogWarning($"User {userId} is blacklisted");
				throw new RpcException(new Status(StatusCode.FailedPrecondition, $"User is blacklisted"));
			}
		}
		else
		{
			// Обработка ошибки
			throw new RpcException(new Status(StatusCode.NotFound, $"User not found in monolith API"));
		}
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

using BookingMicroService.Grpc;
using BookingService.Models;
using BookingService.Models.MonolothDtos;
using Confluent.Kafka;
using Google.Protobuf;
using Grpc.Core;
using System;
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
		await ValidateHotel(request.HotelId);

		var basePrice = await ResolveBasePrice(request.UserId);
		var discountPercent = await ResolvePromoDiscountPercent(request.PromoCode, request.UserId);

		var finalPrice = basePrice * (1 - discountPercent);

		var newBooking = new Booking
		{
			UserId = request.UserId,
			HotelId = request.HotelId,
			PromoCode = request.PromoCode,
			DiscountPercent = discountPercent,
			Price = finalPrice,
			CreatedAt = DateTimeOffset.Now,
		};

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
		var userDto = await GetRest<User>($"http://monolith:8080/api/users/{userId}");

		if (!userDto.active)
		{
			_logger.LogWarning($"User {userId} is inactive");
			throw new RpcException(new Status(StatusCode.FailedPrecondition, $"User is inactive"));
		}

		if (userDto.blacklisted)
		{
			_logger.LogWarning($"User {userId} is blacklisted");
			throw new RpcException(new Status(StatusCode.FailedPrecondition, $"User is blacklisted"));
		}
	}

	private async Task ValidateHotel(string hotelId)
	{
		var isOperational = await GetRest<bool>($"http://monolith:8080/api/hotels/{hotelId}/operational");
		if (isOperational == false)
		{
			_logger.LogWarning($"Hotel {hotelId} is not operational");
			throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Hotel is not operational"));
		}

		var isTrusted = await GetRest<bool>($"http://monolith:8080/api/reviews/hotel/{hotelId}/trusted");
		if (isTrusted == false)
		{
			_logger.LogWarning($"Hotel {hotelId} is not trusted");
			throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Hotel is not trusted"));
		}
	
		var isFullyBooked = await GetRest<bool>($"http://monolith:8080/api/hotels/{hotelId}/fully-booked");
		if (isFullyBooked == true)
		{
			_logger.LogWarning($"Hotel {hotelId} is fully booked");
			throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Hotel is fully booked"));
		}
	}
	private async Task<decimal> ResolveBasePrice( string userId)
	{
		var isVip = await GetRest<bool>($"http://monolith:8080/api/users/{userId}/vip");
		var basePrice = isVip ? 80.0m : 100.0m;

		_logger.LogDebug(@"User status is vip: '{0}', base price is {1}", isVip, basePrice);
		
		return basePrice;
	}

	private async Task<decimal> ResolvePromoDiscountPercent(String promoCode, string userId)
	{
		if (promoCode == null)
		{
			return 0.0m;
		}

		var code = await PostRest<Promocode>($"http://monolith:8080/api/promos/validate?code={promoCode}&userId={userId}");

		return code.discountPercent;
	}

	private async Task<T> ProcessResponseMessage<T>(HttpResponseMessage msg)
	{
		if (!msg.IsSuccessStatusCode)
		{
			throw new RpcException(
				new Status( 
						StatusCode.NotFound, 
						$"Failed to get succesful response from {msg.RequestMessage.Method} request to {msg.RequestMessage.RequestUri}. Response status code is {msg.StatusCode}"));
		}

		var dataJson = await msg.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<T>(dataJson);

		if (result == null)
		{
			_logger.LogError($"Failed to deserialize response from {msg.RequestMessage.Method} request to {msg.RequestMessage.RequestUri}");
			throw new RpcException(new Status(StatusCode.Internal, $"Invalid data format received from external API from {msg.RequestMessage.Method} request to {msg.RequestMessage.RequestUri}"));
		}

		return result;
	}

	private async Task<T> GetRest<T>(string url)
	{
		var httpClient = _clientFactory.CreateClient();

		var msg = await httpClient.GetAsync(url);

		return await ProcessResponseMessage<T>(msg);
	}

	private async Task<T> PostRest<T>(string url)
	{
		var httpClient = _clientFactory.CreateClient();

		var msg = await httpClient.PostAsync(url, null);
		
		return await ProcessResponseMessage<T>(msg);
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

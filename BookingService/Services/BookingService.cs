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
	private readonly RestService _rest;
	private readonly UserService _userService;
	private readonly IConfiguration _configuration;
	private readonly ILogger<BookingService> _logger;

	public BookingService(IConfiguration configuration, ILogger<BookingService> logger, RestService rest, UserService userService)
	{
		_configuration = configuration;
		_logger = logger;
		_rest = rest;
		_userService = userService;
	}

	private async Task<Booking> ResolveBooking(BookingRequest request)
	{
		await ValidateUser(request.UserId);
		await ValidateHotel(request.HotelId);

		var basePrice = await ResolveBasePrice(request.UserId);
		var discountPercent = await ResolvePromoDiscountPercent(request.PromoCode, request.UserId);

		var finalPrice = basePrice * (1 - discountPercent / 100);

		return new Booking
		{
			UserId = request.UserId,
			HotelId = request.HotelId,
			PromoCode = request.PromoCode,
			DiscountPercent = discountPercent,
			Price = finalPrice,
			CreatedAt = DateTimeOffset.Now,
		};
	}

	// --- Реализация gRPC метода CreateBooking ---
	public override async Task<BookingResponse> CreateBooking(BookingRequest request, ServerCallContext context)
	{
		_logger.LogInformation($"Received booking request for user {request.UserId} hotel {request.HotelId}");

		// В реальном приложении здесь была бы логика бизнес-валидации,
		// взаимодействие с базой данных и расчет цены/скидки.

		var newBooking = await ResolveBooking(request);

		// Подготовка сообщения для Kafka (можно использовать BookingResponse как формат события)
		var bookingEvent = new BookingResponse
		{
			Id = newBooking.Id.ToString(),
			UserId = newBooking.UserId,
			HotelId = newBooking.HotelId,
			PromoCode = newBooking.PromoCode,
			DiscountPercent = (double)newBooking.DiscountPercent,
			Price = (double)newBooking.Price,
			CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(newBooking.CreatedAt).ToString() // Используем ISO-8601 строку
		};

		// Отправка сообщения в Kafka асинхронно
		await ProduceKafkaMessageAsync("hotel-booking-events", bookingEvent);

		// Возвращаем ответ gRPC клиенту
		return bookingEvent;
	}

	private async Task ValidateUser(string userId)
	{
		var validationResult = await _userService.ValidateUserForBooking(userId);

		if (!validationResult.IsValid)
		{
			_logger.LogWarning($"User {userId} is invalid for booking: {validationResult.Message}");
			throw new RpcException(new Status(StatusCode.FailedPrecondition, validationResult.Message));
		}
	}

	private async Task ValidateHotel(string hotelId)
	{
		var isOperational = await _rest.GetRest<bool>($"/api/hotels/{hotelId}/operational");
		if (isOperational == false)
		{
			_logger.LogWarning($"Hotel {hotelId} is not operational");
			throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Hotel is not operational"));
		}

		var isTrusted = await _rest.GetRest<bool>($"/api/reviews/hotel/{hotelId}/trusted");
		if (isTrusted == false)
		{
			_logger.LogWarning($"Hotel {hotelId} is not trusted");
			throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Hotel is not trusted"));
		}
	
		var isFullyBooked = await _rest.GetRest<bool>($"/api/hotels/{hotelId}/fully-booked");
		if (isFullyBooked == true)
		{
			_logger.LogWarning($"Hotel {hotelId} is fully booked");
			throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Hotel is fully booked"));
		}
	}
	private async Task<decimal> ResolveBasePrice( string userId)
	{
		var isVip = await _rest.GetRest<bool>($"/api/users/{userId}/vip");
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

		var code = await _rest.PostRest<Promocode>($"/api/promos/validate?code={promoCode}&userId={userId}");

		return code.discountPercent;
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
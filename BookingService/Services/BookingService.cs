using BookingMicroService.Grpc;
using BookingService.Models;
using BookingService.Models.MonolothDtos;
using BookingService.Repositories;
using Confluent.Kafka;
using Google.Protobuf;
using Grpc.Core;
using System;
using System.Text.Json;

namespace BookingService.Services;

public class BookingService : BookingMicroService.Grpc.BookingService.BookingServiceBase
{
	private readonly BookingRepository _bookingRepo;
	private readonly UserService _userService;
	private readonly HotelService _hotelService;
	private readonly PromoService _promoService;
	private readonly IConfiguration _configuration;
	private readonly ILogger<BookingService> _logger;

	public BookingService(BookingRepository bookingRepo, UserService userService, HotelService hotelService, PromoService promoService, IConfiguration configuration, ILogger<BookingService> logger)
	{
		_bookingRepo = bookingRepo;
		_userService = userService;
		_hotelService = hotelService;
		_promoService = promoService;
		_configuration = configuration;
		_logger = logger;
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
	private BookingResponse MapBookingResponse(Booking newBooking)
	{
		return new BookingResponse
		{
			Id = newBooking.Id.ToString(),
			UserId = newBooking.UserId,
			HotelId = newBooking.HotelId,
			PromoCode = newBooking.PromoCode ?? "",
			DiscountPercent = (double)newBooking.DiscountPercent,
			Price = (double)newBooking.Price,
			CreatedAt = newBooking.CreatedAt.ToString("O")
		};
	}
	// --- Реализация gRPC метода CreateBooking ---
	public override async Task<BookingResponse> CreateBooking(BookingRequest request, ServerCallContext context)
	{
		_logger.LogInformation($"Received booking request for user {request.UserId} hotel {request.HotelId}");

		var newBooking = await ResolveBooking(request);
		await _bookingRepo.SaveBooking(newBooking);

		var bookingEvent = MapBookingResponse(newBooking);

//		await ProduceKafkaMessageAsync("hotel-booking-events", bookingEvent);

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
		var validationResult = await _hotelService.ValidateHotelForBooking(hotelId);

		if (!validationResult.IsValid)
		{
			_logger.LogWarning($"Hotel {hotelId} is invalid for booking: {validationResult.Message}");
			throw new RpcException(new Status(StatusCode.FailedPrecondition, validationResult.Message));
		}
	}
	private async Task<decimal> ResolveBasePrice( string userId)
	{
		var isVip = await _userService.IsUserVip(userId);
		var basePrice = isVip ? 80.0m : 100.0m;

		_logger.LogDebug(@"User status is vip: '{0}', base price is {1}", isVip, basePrice);
		
		return basePrice;
	}

	private async Task<decimal> ResolvePromoDiscountPercent(String promoCode, string userId)
	{
		if (String.IsNullOrWhiteSpace(promoCode))
		{
			return 0.0m;
		}

		var result = await _promoService.ResolvePromoDiscount(promoCode, userId);
		_logger.LogDebug($"Promocode {promoCode} applied discount percent: '{result}'");

		return result;
	}

	// --- Реализация gRPC метода ListBookings (заглушка) ---
	public override async Task<BookingListResponse> ListBookings(BookingListRequest request, ServerCallContext context)
	{
		_logger.LogInformation($"Received list request for user {request.UserId}");

		var response = new BookingListResponse();
		var bookings = await _bookingRepo.Get(request.UserId);
		
		foreach (var booking in bookings)
		{
			response.Bookings.Add(MapBookingResponse(booking));
		}
		
		return response;
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
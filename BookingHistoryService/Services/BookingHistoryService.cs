using BookingHistoryService.Data;
using BookingHistoryService.Models;
using BookingMicroService.Grpc;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BookingHistoryService.Services
{
	public class BookingHistoryService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<BookingHistoryService> _logger;

		public BookingHistoryService(IServiceProvider serviceProvider, ILogger<BookingHistoryService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		public async Task ProcessKafkaMessage(string json)
		{
			try
			{
				var eventData = JsonSerializer.Deserialize<BookingResponse>(json);

				if (eventData == null) return;

				using (var scope = _serviceProvider.CreateScope())
				{
					var context = scope.ServiceProvider.GetRequiredService<BookingHistoryContext>();

					var factBooking = new Booking
					{
						Id = long.Parse(eventData.Id),
						UserId = eventData.UserId,
						HotelId = eventData.HotelId,
						PromoCode = eventData.PromoCode,
						DiscountPercent = (decimal)eventData.DiscountPercent,
						Price = (decimal)eventData.Price,
						CreatedAt = DateTimeOffset.Parse(eventData.CreatedAt),
					};

					if (await context.FactBookings.AnyAsync(x => x.Id == factBooking.Id))
					{
						_logger.LogInformation($"Booking {factBooking.Id} is already saved.");
						return;
					}

					context.FactBookings.Add(factBooking);
					await context.SaveChangesAsync();
					_logger.LogInformation($"Booking {factBooking.Id} is saved.");
				}
			}
			catch(Exception ex)
			{
				_logger.LogError($"{ex.Message} : {json}");
				throw;
			}

		}
	}
}

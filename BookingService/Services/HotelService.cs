using BookingService.Models;
using Grpc.Core;

namespace BookingService.Services
{
	public class HotelService
	{
		private readonly RestService _rest;

		public HotelService(RestService rest)
		{
			_rest = rest;
		}

		public async Task<ValidationResult> ValidateHotelForBooking(string hotelId)
		{
			var isOperational = await _rest.GetRest<bool>($"/api/hotels/{hotelId}/operational");
			if (isOperational == false)
			{
				return ValidationResult.Failed("Hotel is not operational");
			}

			var isTrusted = await _rest.GetRest<bool>($"/api/reviews/hotel/{hotelId}/trusted");
			if (isTrusted == false)
			{
				return ValidationResult.Failed("Hotel is not trusted");
			}

			var isFullyBooked = await _rest.GetRest<bool>($"/api/hotels/{hotelId}/fully-booked");
			if (isFullyBooked == true)
			{
				return ValidationResult.Failed("Hotel is fully booked");
			}

			return ValidationResult.Sucessful();
		} 	
	}
}

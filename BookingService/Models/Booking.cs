namespace BookingService.Models
{
	public class Booking
	{
		public int Id { get; set; }
		public string UserId { get; set; }
		public string HotelId { get; set; }
		public string? PromoCode { get; set; }

		public double DiscountPercent { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}

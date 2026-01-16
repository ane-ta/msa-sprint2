namespace BookingHistoryService.Models.DTOs
{
	public class StatisticsDailyBookingsDto
	{
		public DateOnly Date { get; set;}
		public int BookingsCount { get; set; }

		public decimal TotalBookingAmount { get; set; }
		public decimal AverageBookingAmount { get; set; }
	}
}

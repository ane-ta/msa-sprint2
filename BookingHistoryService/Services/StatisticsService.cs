using BookingHistoryService.Data;
using BookingHistoryService.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BookingHistoryService.Services
{
	public class StatisticsService
	{
		private readonly BookingHistoryContext _dbContext;

		public StatisticsService(BookingHistoryContext context)
		{
			_dbContext = context;
		}
		public async Task<IEnumerable<StatisticsDailyBookingsDto>> GetDailyBookingStatisticsAsync( DateOnly? date)
		{
			var stats = await _dbContext.FactBookings
				.GroupBy(b => b.CreatedAt.Date) 
				.Select(g => new StatisticsDailyBookingsDto
				{
					Date = DateOnly.FromDateTime(g.Key.Date), 
					BookingsCount = g.Count(), 
					TotalBookingAmount = g.Sum(b => b.Price), 
					AverageBookingAmount = (decimal)g.Average(b => b.Price) 
				})
				.OrderBy(dto => dto.Date) 
				.ToListAsync();

			return stats;
		}
	}
}

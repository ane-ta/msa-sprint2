using BookingHistoryService.Models.DTOs;
using BookingHistoryService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingHistoryService.Controllers
{
	[ApiController]
	[Route("api/statistics")]
	public class StatisticsController : ControllerBase
	{
		private readonly StatisticsService _statisticsService;

		public StatisticsController(StatisticsService statisticsService)
		{
			_statisticsService = statisticsService;
		}

		[HttpGet("daily")]
		public async Task<ActionResult<IEnumerable<StatisticsDailyBookingsDto>>> GetDailyStats()
		{
			var stats = await _statisticsService.GetDailyBookingStatisticsAsync( null);
			return Ok(stats);
		}
	}
}

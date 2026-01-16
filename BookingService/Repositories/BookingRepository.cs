using BookingService.Data;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Repositories
{
	public class BookingRepository
	{
		private readonly BookingContext _context;
		public BookingRepository(BookingContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<Booking>> Get(string userId)
		{
			if(String.IsNullOrWhiteSpace(userId))
			{
				return await _context.Bookings.ToListAsync();
			}

			return await _context
				.Bookings.Where(x => x.UserId == userId)
				.ToListAsync();
		}

		public async Task SaveBooking(Booking booking)
		{
			_context.Bookings.Add(booking);
			await _context.SaveChangesAsync();
		} 
	}
}

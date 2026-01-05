using BookingHistoryService.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BookingHistoryService.Data
{
	public class BookingHistoryContext : DbContext
	{
		public BookingHistoryContext(DbContextOptions<BookingHistoryContext> options) : base(options)
		{
		}

		// Это свойство представляет вашу таблицу в базе данных
		public DbSet<Booking> FactBookings { get; set; } = null!;
	}
}

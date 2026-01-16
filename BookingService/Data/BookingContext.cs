using Microsoft.EntityFrameworkCore;
using BookingService.Models;

namespace BookingService.Data
{
	public class BookingContext : DbContext
	{
		// Конструктор необходим для передачи опций (например, строки подключения)
		public BookingContext(DbContextOptions<BookingContext> options) : base(options)
		{
		}

		// Это свойство представляет вашу таблицу в базе данных
		public DbSet<Booking> Bookings { get; set; } = null!;
	}
}

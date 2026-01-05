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

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Booking>()
				.Property(f => f.Id)
				.ValueGeneratedNever();
			
			base.OnModelCreating(modelBuilder);
		}
	}
}

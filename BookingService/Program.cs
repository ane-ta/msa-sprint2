using BookingService.Data;
using BookingService.Repositories;
using BookingService.Services;
using Confluent.Kafka;
using KafkaLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// !!! ВАЖНО ДЛЯ DOCKER !!! 
// Разрешаем HTTP/2 без шифрования (TLS) для работы внутри Docker сети.
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddHttpClient();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Регистрируем DbContext с использованием Npgsql (PostgreSQL)
builder.Services.AddDbContext<BookingContext>(options =>
	options.UseNpgsql(connectionString));

builder.Services.AddTransient<RestService>();
builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<HotelService>();
builder.Services.AddTransient<PromoService>();
builder.Services.AddTransient<BookingRepository>();
builder.Services.AddSingleton<IKafkaProduceService, KafkaStringProduceService>(sp =>
{
	var configuration = sp.GetRequiredService<IConfiguration>();

	var config = new ProducerConfig { BootstrapServers = configuration["Kafka:BootstrapServers"] };

	var logger = sp.GetRequiredService<ILogger<KafkaStringProduceService>>();

	return new KafkaStringProduceService(config, logger);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<BookingContext>();
	// Применяем все ожидающие миграции к базе данных при запуске приложения
	Console.WriteLine("Applying database migrations...");
	dbContext.Database.Migrate();
	Console.WriteLine("Database migrations applied successfully.");
}

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<BookingService.Services.BookingService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

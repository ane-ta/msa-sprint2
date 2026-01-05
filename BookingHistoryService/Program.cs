using BookingHistoryService.Data;
using BookingHistoryService.Services;
using Confluent.Kafka;
using KafkaLibrary;
using Microsoft.EntityFrameworkCore;

// !!! ВАЖНО ДЛЯ DOCKER !!! 
// Разрешаем HTTP/2 без шифрования (TLS) для работы внутри Docker сети.
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

builder.Services.AddDbContext<BookingHistoryContext>(options =>
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("DefaultConnection")
));

builder.Services.AddSingleton<IKafkaConsumeService, KafkaStringConsumeService>(sp =>
{
	var configuration = sp.GetRequiredService<IConfiguration>();

	ArgumentNullException.ThrowIfNullOrWhiteSpace(configuration["Kafka:GroupId"], "Kafka:GroupId");
	ArgumentNullException.ThrowIfNullOrWhiteSpace(configuration["Kafka:BootstrapServers"], "Kafka:BootstrapServers");

	var config = new ConsumerConfig
	{
		GroupId = configuration["Kafka:GroupId"],
		BootstrapServers = configuration["Kafka:BootstrapServers"],
		AutoOffsetReset = AutoOffsetReset.Earliest,
		SessionTimeoutMs = 6000
	};

	var logger = sp.GetRequiredService<ILogger<KafkaStringConsumeService>>();

	var consumer = new KafkaStringConsumeService(config, logger);
	consumer.OnMessageReceived += async (key, value) =>
	{
		Console.WriteLine($"Received message: Key='{key}', Value='{value}'");
		
		await Task.CompletedTask;
	};

	return consumer;
});

builder.Services.AddHostedService<KafkaConsumeBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<BookingHistoryContext>();
	// Применяем все ожидающие миграции к базе данных
	dbContext.Database.Migrate();
}

app.Run();

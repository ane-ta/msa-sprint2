using BookingHistoryService.Services;
using Confluent.Kafka;
using KafkaLibrary;

// !!! ВАЖНО ДЛЯ DOCKER !!! 
// Разрешаем HTTP/2 без шифрования (TLS) для работы внутри Docker сети.
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

builder.Services.AddSingleton<IKafkaConsumeService, KafkaStringConsumeService>(sp =>
{
	var configuration = sp.GetRequiredService<IConfiguration>();

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

app.Run();

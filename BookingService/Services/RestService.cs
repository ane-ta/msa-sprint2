using Grpc.Core;
using System.Text.Json;

namespace BookingService.Services
{
	public class RestService
	{
		private readonly IConfiguration _configuration;
		private readonly IHttpClientFactory _clientFactory;
		private readonly ILogger<RestService> _logger;

		public RestService(IConfiguration configuration, IHttpClientFactory clientFactory, ILogger<RestService> logger)
		{
			_configuration = configuration;
			_clientFactory = clientFactory;
			_logger = logger;
		}

		private async Task<T> ProcessResponseMessage<T>(HttpResponseMessage msg)
		{
			if (!msg.IsSuccessStatusCode)
			{
				throw new RpcException(
					new Status(
							StatusCode.NotFound,
							$"Failed to get succesful response from {msg.RequestMessage.Method} request to {msg.RequestMessage.RequestUri}. Response status code is {msg.StatusCode}"));
			}

			var dataJson = await msg.Content.ReadAsStringAsync();
			var result = JsonSerializer.Deserialize<T>(dataJson);

			if (result == null)
			{
				_logger.LogError($"Failed to deserialize response from {msg.RequestMessage.Method} request to {msg.RequestMessage.RequestUri}");
				throw new RpcException(new Status(StatusCode.Internal, $"Invalid data format received from external API from {msg.RequestMessage.Method} request to {msg.RequestMessage.RequestUri}"));
			}

			return result;
		}

		public async Task<T> GetRest<T>(string url)
		{
			var httpClient = _clientFactory.CreateClient();

			var msg = await httpClient.GetAsync(url);

			return await ProcessResponseMessage<T>(msg);
		}

		public async Task<T> PostRest<T>(string url)
		{
			var httpClient = _clientFactory.CreateClient();

			var msg = await httpClient.PostAsync(url, null);

			return await ProcessResponseMessage<T>(msg);
		}
	}
}

using BookingService.Models.MonolothDtos;

namespace BookingService.Services
{
	public class PromoService
	{
		private readonly RestService _rest;
		private readonly ILogger<UserService> _logger;

		public PromoService(RestService rest, ILogger<UserService> logger)
		{
			_rest = rest;
			_logger = logger;
		}

		public async Task<decimal> ResolvePromoDiscount(string promoCode, string userId)
		{
			var code = await _rest.PostNullableRest<Promocode>($"/api/promos/validate?code={promoCode}&userId={userId}");

			return code == null ? 0 : code.discountPercent;
		}
	}
}

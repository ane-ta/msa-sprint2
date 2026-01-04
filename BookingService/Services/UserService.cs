using BookingService.Models;
using BookingService.Models.MonolothDtos;
using System.ComponentModel;

namespace BookingService.Services
{
	public class UserService
	{
		private readonly IConfiguration _configuration;
		private readonly RestService _rest;
		private readonly ILogger<UserService> _logger;

		public UserService(IConfiguration configuration, RestService rest, ILogger<UserService> logger)
		{
			_configuration = configuration;
			_rest = rest;
			_logger = logger;
		}

		private async Task<User> GetById(string userId)
		{
			return await _rest.GetRest<User>($"/api/users/{userId}");
		}

		private ValidationResult ValidateForBooking(User userDto)
		{
			if (!userDto.active)
			{
				return ValidationResult.Failed("User is inactive");
			}

			if (userDto.blacklisted)
			{
				return ValidationResult.Failed("User is blacklisted");
			}

			return ValidationResult.Sucessful();
		}

		public async Task<bool> IsUserVip(string userId)
		{
			return await _rest.GetRest<bool>($"/api/users/{userId}/vip");
		}

		public async Task<ValidationResult> ValidateUserForBooking(string userId)
		{
			var userDto = await GetById(userId);
			return ValidateForBooking(userDto);
		}
	}
}

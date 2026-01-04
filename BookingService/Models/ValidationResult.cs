namespace BookingService.Models
{
	public record ValidationResult(bool IsValid, string Message)
	{
		public static ValidationResult Sucessful() => new ValidationResult(true, "");
		public static ValidationResult Failed(string message) => new ValidationResult(false, message);
	};
}

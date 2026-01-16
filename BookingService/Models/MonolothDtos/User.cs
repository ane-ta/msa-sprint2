namespace BookingService.Models.MonolothDtos
{
	public class User
	{
		public string id { get; set; }
		public string status { get; set; }
		public bool blacklisted { get; set; }
		public bool active { get; set; }
	}
}

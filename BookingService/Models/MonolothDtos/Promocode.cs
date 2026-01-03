namespace BookingService.Models.MonolothDtos
{
	public class Promocode
	{
		public string code { get; set; }
		public decimal discountPercent { get; set; }
		public bool active { get; set; }
		public bool vipOnly { get; set; }
	}
}

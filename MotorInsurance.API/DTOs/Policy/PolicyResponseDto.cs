namespace MotorInsurance.API.DTOs.Policy
{
    public class PolicyResponseDto
    {
        public int Id { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int QuoteId { get; set; }

        public string ClientName { get; set; }
        public string CarBrand { get; set; }
        public string CarModel { get; set; }

        public decimal Price { get; set; }
    }
}
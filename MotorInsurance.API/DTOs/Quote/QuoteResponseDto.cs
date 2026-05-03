using MotorInsurance.API.Common;

namespace MotorInsurance.API.DTOs.Quote
{
    public class QuoteResponseDto
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public QuoteStatus Status { get; set; }
    }
}

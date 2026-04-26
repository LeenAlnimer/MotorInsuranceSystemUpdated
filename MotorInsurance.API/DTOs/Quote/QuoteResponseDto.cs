using System;

namespace MotorInsurance.API.DTOs.Quote
{
    public class QuoteResponseDto
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
    }
}
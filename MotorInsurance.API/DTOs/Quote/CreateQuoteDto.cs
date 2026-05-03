using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.Quote
{
    public class CreateQuoteDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "CarId must be a valid ID")]
        public int CarId { get; set; }
    }
}
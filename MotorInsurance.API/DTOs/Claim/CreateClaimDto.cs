using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.Claim
{
    public class CreateClaimDto
    {
        [Required, MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int PolicyId { get; set; }

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Claim amount must be greater than zero")]
        public decimal ClaimAmount { get; set; }
    }
}

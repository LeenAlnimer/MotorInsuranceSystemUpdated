using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.Claim
{
    public class CreateClaimDto
    {
        [Required]
        public string Description { get; set; }

        public string? Status { get; set; }

        [Required]
        public int PolicyId { get; set; }

        [Required]
        public int UserId { get; set; }
    }
}
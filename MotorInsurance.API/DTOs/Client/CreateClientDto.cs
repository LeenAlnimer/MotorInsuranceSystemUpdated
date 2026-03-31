using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.Client
{
    public class CreateClientDto
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
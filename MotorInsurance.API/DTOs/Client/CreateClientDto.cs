using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.Client
{
    public class CreateClientDto
    {
        [Required, MinLength(2)]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [RegularExpression(@"^07[789]\d{7}$", ErrorMessage = "Invalid Jordanian phone number")]
        public string? PhoneNumber { get; set; }
    }
}
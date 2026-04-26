using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.Client
{
    public class UpdateClientDto
    {
        [MinLength(2)]
        public string? FullName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [RegularExpression(@"^07[789]\d{7}$", ErrorMessage = "Invalid Jordanian phone number")]
        public string? PhoneNumber { get; set; }
    }
}
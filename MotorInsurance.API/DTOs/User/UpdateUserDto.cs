using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.User
{
    public class UpdateUserDto
    {
        [MinLength(3)]
        public string? Username { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [RegularExpression(@"^07[789]\d{7}$")]
        public string? PhoneNumber { get; set; }

        public string? Password { get; set; }
    }
}
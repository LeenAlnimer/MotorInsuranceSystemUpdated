using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.User
{
    public class UpdateUserDto
    {
        [MinLength(2)]
        public string? FullName { get; set; }

        [MinLength(3)]
        public string? Username { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [RegularExpression(@"^07[789]\d{7}$")]
        public string? PhoneNumber { get; set; }

        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W]).{6,}$",
            ErrorMessage = "Password must be at least 6 characters with an uppercase letter, a digit, and a special character")]
        public string? Password { get; set; }
    }
}
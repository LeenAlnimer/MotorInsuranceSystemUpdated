using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.User
{
    public class CreateUserDto
    {
        [Required, MinLength(3)]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^07[789]\d{7}$")]
        public string PhoneNumber { get; set; }

        [Required]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W]).{6,}$")]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}
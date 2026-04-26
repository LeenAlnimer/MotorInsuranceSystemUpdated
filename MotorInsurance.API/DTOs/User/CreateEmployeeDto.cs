using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.User
{
    public class CreateEmployeeDto
    {
        [Required, MinLength(3)]
        public string Username { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [RegularExpression(@"^07[789]\d{7}$")]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W]).{6,}$")]
        public string Password { get; set; } = null!;
    }
}

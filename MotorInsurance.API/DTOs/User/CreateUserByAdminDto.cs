using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.User
{
    public class CreateUserByAdminDto
    {
        [MinLength(2)]
        public string? FullName { get; set; }

        [Required, MinLength(3)]
        public string Username { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [RegularExpression(@"^07[789]\d{7}$", ErrorMessage = "Phone must be a valid Jordanian number")]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W]).{6,}$", ErrorMessage = "Password must have uppercase, digit, and special character")]
        public string Password { get; set; } = null!;

        [Required]
        [RegularExpression("^(Admin|Employee|Client)$", ErrorMessage = "Role must be Admin, Employee, or Client")]
        public string Role { get; set; } = "Employee";
    }
}

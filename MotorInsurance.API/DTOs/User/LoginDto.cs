using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.User
{
    public class LoginDto
    {
        [Required]
        public string EmailOrPhone { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}
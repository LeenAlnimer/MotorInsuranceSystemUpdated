using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.User
{
    public class LoginDto
    {
        [Required]
        public string Identifier { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
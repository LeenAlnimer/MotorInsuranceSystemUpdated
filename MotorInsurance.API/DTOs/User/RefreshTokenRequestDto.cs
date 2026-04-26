using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.User
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}

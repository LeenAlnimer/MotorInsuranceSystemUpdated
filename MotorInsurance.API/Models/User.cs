using System.Text.Json.Serialization;

namespace MotorInsurance.API.Models
{
    public class User
    {
        public int Id { get; set; }

        public string? Username { get; set; }
        public string? Password { get; set; }

        
        public string Role { get; set; } = "Employee";

        public List<Claim>? Claims { get; set; }
        public List<RefreshToken>? RefreshTokens { get; set; }
    }
}
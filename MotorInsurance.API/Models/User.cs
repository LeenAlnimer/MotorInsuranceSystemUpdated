using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MotorInsurance.API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MinLength(3)]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^07[789]\d{7}$")]
        public string PhoneNumber { get; set; }

        [Required]
        [JsonIgnore] 
        public string PasswordHash { get; set; }

        public string Role { get; set; } = "User";

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }
    }
}
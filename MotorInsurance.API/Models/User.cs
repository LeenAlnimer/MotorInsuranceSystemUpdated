using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MotorInsurance.API.Models
{
    public class User
    {
        public int Id { get; set; }

        [MinLength(2)]
        public string? FullName { get; set; }

        [Required, MinLength(3)]
        public string Username { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [RegularExpression(@"^07[789]\d{7}$")]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        [JsonIgnore]
        public string PasswordHash { get; set; } = null!;

        public string Role { get; set; } = "Client";

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public List<Car>? Cars { get; set; }
    }
}

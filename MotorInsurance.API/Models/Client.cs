using System.Text.Json.Serialization;

namespace MotorInsurance.API.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public int? UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        public List<Car>? Cars { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}

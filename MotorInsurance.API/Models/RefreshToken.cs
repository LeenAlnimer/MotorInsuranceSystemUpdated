using System.Text.Json.Serialization;

namespace MotorInsurance.API.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public string Token { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }

        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }
    }
}
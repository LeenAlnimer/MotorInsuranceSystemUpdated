using System.Text.Json.Serialization;

namespace MotorInsurance.API.Models
{
    public class Claim
    {
        public int Id { get; set; }

        public string? Description { get; set; }
        public string? Status { get; set; }

        public int PolicyId { get; set; }

        [JsonIgnore]
        public Policy? Policy { get; set; }

        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }
    }
}
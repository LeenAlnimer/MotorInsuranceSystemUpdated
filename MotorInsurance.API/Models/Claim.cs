using System.Text.Json.Serialization;
using MotorInsurance.API.Common;

namespace MotorInsurance.API.Models
{
    public class Claim
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int PolicyId { get; set; }

        [JsonIgnore]
        public Policy? Policy { get; set; }

        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }
    }
}

using System.Text.Json.Serialization;
using MotorInsurance.API.Common;

namespace MotorInsurance.API.Models
{
    public class Claim
    {
        public int Id { get; set; }
        public string Description { get; set; } = null!;
        public decimal ClaimAmount { get; set; }
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int PolicyId { get; set; }

        [JsonIgnore]
        public Policy? Policy { get; set; }

        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public int? RejectedById { get; set; }
        public DateTime? RejectedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}

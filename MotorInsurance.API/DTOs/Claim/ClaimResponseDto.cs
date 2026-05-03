using MotorInsurance.API.Common;

namespace MotorInsurance.API.DTOs.Claim
{
    public class ClaimResponseDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = null!;
        public decimal ClaimAmount { get; set; }
        public ClaimStatus Status { get; set; }
        public int PolicyId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? RejectedById { get; set; }
        public DateTime? RejectedAt { get; set; }
    }
}
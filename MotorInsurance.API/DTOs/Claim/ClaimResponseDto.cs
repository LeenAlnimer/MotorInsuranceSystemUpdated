namespace MotorInsurance.API.DTOs.Claim
{
    public class ClaimResponseDto
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public int PolicyId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
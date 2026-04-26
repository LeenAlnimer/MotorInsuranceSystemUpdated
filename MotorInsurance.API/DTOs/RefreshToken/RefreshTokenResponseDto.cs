namespace MotorInsurance.API.DTOs.RefreshToken
{
    public class RefreshTokenResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiryDate;
    }
}

namespace MotorInsurance.API.DTOs.Client
{
    public class UpdateClientDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
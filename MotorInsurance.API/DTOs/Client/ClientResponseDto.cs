namespace MotorInsurance.API.DTOs.Client
{
    public class ClientResponseDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public List<CarDto>? Cars { get; set; }
    }

    public class CarDto
    {
        public int Id { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
    }
}
namespace MotorInsurance.API.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public List<Car>? Cars { get; set; }
    }
}
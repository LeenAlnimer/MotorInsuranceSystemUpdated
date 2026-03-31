namespace MotorInsurance.API.DTOs.Car
{
    public class CarResponseDto
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public decimal Price { get; set; }
    }
}
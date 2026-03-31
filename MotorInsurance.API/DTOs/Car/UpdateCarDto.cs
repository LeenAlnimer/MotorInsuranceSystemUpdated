using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.Car
{
    public class UpdateCarDto
    {
        [Required]
        public string Brand { get; set; }

        [Required]
        public string Model { get; set; }

        public int Year { get; set; }

        public decimal Price { get; set; }

        public string FuelType { get; set; }

        public int ClientId { get; set; }
    }
}
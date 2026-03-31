using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.Car
{
    public class CreateCarDto
    {
        [Required]
        public string Brand { get; set; }

        [Required]
        public string Model { get; set; }

        [Range(1900, 2100)]
        public int Year { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public string FuelType { get; set; }

        public int ClientId { get; set; }
    }
}
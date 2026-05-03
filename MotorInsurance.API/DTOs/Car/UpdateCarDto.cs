using System.ComponentModel.DataAnnotations;
using MotorInsurance.API.Common;

namespace MotorInsurance.API.DTOs.Car
{
    public class UpdateCarDto
    {
        public string? Brand { get; set; }

        public string? Model { get; set; }

        [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100")]
        public int? Year { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal? Price { get; set; }

        [EnumDataType(typeof(FuelType))]
        public FuelType? FuelType { get; set; }
    }
}

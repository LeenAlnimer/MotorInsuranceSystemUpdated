using System.Collections;
using System.Text.Json.Serialization;

namespace MotorInsurance.API.Models
{
    public class Car
    {
        public int Id { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int Year { get; set; }
        public decimal Price { get; set; }
        public string? FuelType { get; set; }

        public int ClientId { get; set; }

        [JsonIgnore]
        public Client? Client { get; set; }

        public List<Quote>? Quotes { get; set; }
    }
}
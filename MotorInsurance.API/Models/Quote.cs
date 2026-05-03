using System.Text.Json.Serialization;
using MotorInsurance.API.Common;

namespace MotorInsurance.API.Models
{
    public class Quote
    {
        public int Id { get; set; }

        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        public QuoteStatus Status { get; set; } = QuoteStatus.Pending;

        public int CarId { get; set; }

        [JsonIgnore]
        public Car? Car { get; set; }

        public Policy? Policy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}

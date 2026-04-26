using System.Text.Json.Serialization;

namespace MotorInsurance.API.Models
{
    public class Policy
    {
        public int Id { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int QuoteId { get; set; }

        [JsonIgnore]
        public Quote? Quote { get; set; }

        public List<Claim>? Claims { get; set; }
    }
}
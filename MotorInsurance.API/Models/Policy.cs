using System.Text.Json.Serialization;
using MotorInsurance.API.Common;

namespace MotorInsurance.API.Models
{
    public class Policy
    {
        public int Id { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public PolicyStatus Status { get; set; } = PolicyStatus.Active;

        public int QuoteId { get; set; }

        public decimal InsuredValue { get; set; }

        [JsonIgnore]
        public Quote? Quote { get; set; }

        public List<Claim>? Claims { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}

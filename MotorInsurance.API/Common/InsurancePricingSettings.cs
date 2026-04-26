namespace MotorInsurance.API.Common
{
    public class InsurancePricingSettings
    {
        public int MaxCarAgeYears { get; set; } = 10;
        public decimal BaseRatePercent { get; set; } = 0.05m;
        public int NewCarMaxAge { get; set; } = 3;
        public int OldCarMinAge { get; set; } = 8;
        public decimal NewCarMultiplier { get; set; } = 1.2m;
        public decimal OldCarMultiplier { get; set; } = 0.9m;
        public decimal HighPriceThreshold { get; set; } = 30_000m;
        public decimal LowPriceThreshold { get; set; } = 10_000m;
        public decimal HighPriceMultiplier { get; set; } = 1.1m;
        public decimal LowPriceMultiplier { get; set; } = 0.95m;
        public decimal ElectricMultiplier { get; set; } = 0.9m;
        public decimal DieselMultiplier { get; set; } = 1.1m;
        public decimal MinimumPremium { get; set; } = 300m;
    }
}

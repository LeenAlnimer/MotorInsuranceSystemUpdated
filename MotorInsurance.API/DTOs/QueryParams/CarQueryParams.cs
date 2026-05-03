using MotorInsurance.API.Common;

namespace MotorInsurance.API.DTOs.QueryParams
{
    public class CarQueryParams : PaginationParams
    {
        public string? Brand { get; set; }
        public FuelType? FuelType { get; set; }
        public int? MinYear { get; set; }
        public int? MaxYear { get; set; }
    }
}

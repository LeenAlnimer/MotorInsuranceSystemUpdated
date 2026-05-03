using MotorInsurance.API.Common;

namespace MotorInsurance.API.DTOs.QueryParams
{
    public class ClaimQueryParams : PaginationParams
    {
        public ClaimStatus? Status { get; set; }
        public int? UserId { get; set; }
        public int? PolicyId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using MotorInsurance.API.Common;

namespace MotorInsurance.API.DTOs.QueryParams
{
    public class PolicyQueryParams : PaginationParams
    {
        public PolicyStatus? Status { get; set; }
        public string? SortBy { get; set; }     // startDate, endDate
        [RegularExpression("^(asc|desc)$", ErrorMessage = "SortOrder must be 'asc' or 'desc'")]
        public string? SortOrder { get; set; } = "asc";
    }
}

using System.ComponentModel.DataAnnotations;
using MotorInsurance.API.Common;

namespace MotorInsurance.API.DTOs.QueryParams
{
    public class QuoteQueryParams : PaginationParams
    {
        public QuoteStatus? Status { get; set; }
        public string? SortBy { get; set; }     // price, createdAt
        [RegularExpression("^(asc|desc)$", ErrorMessage = "SortOrder must be 'asc' or 'desc'")]
        public string? SortOrder { get; set; } = "asc";
        public bool ExcludeExpired { get; set; } = true;
    }
}

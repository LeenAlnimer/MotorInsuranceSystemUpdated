namespace MotorInsurance.API.DTOs.QueryParams
{
    public class QuoteQueryParams : PaginationParams
    {
        public bool? IsApproved { get; set; }
        public string? SortBy { get; set; }     // price, createdAt
        public string? SortOrder { get; set; } = "asc";
    }
}

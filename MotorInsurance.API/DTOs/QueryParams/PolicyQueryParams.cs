namespace MotorInsurance.API.DTOs.QueryParams
{
    public class PolicyQueryParams : PaginationParams
    {
        public string? SortBy { get; set; }     // startDate, endDate, price
        public string? SortOrder { get; set; } = "asc";
    }
}

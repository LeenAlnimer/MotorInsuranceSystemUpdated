namespace MotorInsurance.API.DTOs.QueryParams
{
    public class UserQueryParams : PaginationParams
    {
        public string? Role { get; set; }
        public string? Search { get; set; }
    }
}

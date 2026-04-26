namespace MotorInsurance.API.DTOs.QueryParams
{
    public class ClientQueryParams : PaginationParams
    {
        public string? Search { get; set; }     // search by name or email
    }
}

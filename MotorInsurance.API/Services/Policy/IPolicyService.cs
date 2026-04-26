using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Policy;
using MotorInsurance.API.DTOs.QueryParams;

namespace MotorInsurance.API.Services.Policy
{
    public interface IPolicyService
    {
        Task<PagedResult<PolicyResponseDto>> GetPagedAsync(PolicyQueryParams queryParams);
        Task<PagedResult<PolicyResponseDto>> GetPagedByClientIdAsync(int clientId, PolicyQueryParams queryParams);
        Task<PolicyResponseDto?> GetByIdAsync(int id);
        Task<(bool Success, string Message)> CancelAsync(int id);
    }
}

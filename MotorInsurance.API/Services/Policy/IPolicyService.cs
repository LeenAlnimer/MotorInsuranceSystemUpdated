using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Policy;
using MotorInsurance.API.DTOs.QueryParams;

namespace MotorInsurance.API.Services.Policy
{
    public interface IPolicyService
    {
        Task<PagedResult<PolicyResponseDto>> GetPagedAsync(PolicyQueryParams queryParams);
        Task<PagedResult<PolicyResponseDto>> GetPagedByUserIdAsync(int userId, PolicyQueryParams queryParams);
        Task<PolicyResponseDto?> GetByIdAsync(int id);
        Task<PolicyResponseDto> RenewAsync(int policyId);
        Task<PolicyResponseDto> CancelAsync(int policyId, int performedByUserId);
    }
}

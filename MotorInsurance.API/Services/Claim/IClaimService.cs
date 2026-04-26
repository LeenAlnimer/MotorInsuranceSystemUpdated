using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Claim;
using MotorInsurance.API.DTOs.QueryParams;

namespace MotorInsurance.API.Services.Claim
{
    public interface IClaimService
    {
        Task<PagedResult<ClaimResponseDto>> GetPagedAsync(ClaimQueryParams queryParams);
        Task<PagedResult<ClaimResponseDto>> GetPagedByUserIdAsync(int userId, ClaimQueryParams queryParams);
        Task<ClaimResponseDto?> GetByIdAsync(int id);
        Task<(bool Success, string Message, ClaimResponseDto? Claim)> CreateAsync(CreateClaimDto dto, int userId);
        Task<bool> ApproveAsync(int id);
        Task<bool> RejectAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}

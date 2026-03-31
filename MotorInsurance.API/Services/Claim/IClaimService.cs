using MotorInsurance.API.DTOs.Claim;

namespace MotorInsurance.API.Services.Claim
{
    public interface IClaimService
    {
        Task<List<ClaimResponseDto>> GetAllAsync();
        Task<(bool Success, string Message, ClaimResponseDto? Claim)> CreateAsync(CreateClaimDto dto);
    }
}
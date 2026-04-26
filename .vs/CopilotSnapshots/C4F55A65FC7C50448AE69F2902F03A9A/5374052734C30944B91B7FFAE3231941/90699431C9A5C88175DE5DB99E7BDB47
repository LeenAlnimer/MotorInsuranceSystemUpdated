using MotorInsurance.API.DTOs.Claim;

namespace MotorInsurance.API.Services.Claim
{
    public interface IClaimService
    {
        Task<List<ClaimResponseDto>> GetAllAsync();
        Task<ClaimResponseDto?> GetByIdAsync(int id);
        Task<(bool Success, string Message, ClaimResponseDto? Claim)> CreateAsync(CreateClaimDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
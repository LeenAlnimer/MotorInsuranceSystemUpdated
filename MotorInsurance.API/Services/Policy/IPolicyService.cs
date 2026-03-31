using MotorInsurance.API.DTOs.Policy;

namespace MotorInsurance.API.Services.Policy
{
    public interface IPolicyService
    {
        Task<List<PolicyResponseDto>> GetAllAsync();
        Task<PolicyResponseDto?> GetByIdAsync(int id);
    }
}
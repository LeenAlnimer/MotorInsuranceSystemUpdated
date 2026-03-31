using MotorInsurance.API.DTOs.Client;

namespace MotorInsurance.API.Services.Client
{
    public interface IClientService
    {
        Task<List<ClientResponseDto>> GetAllAsync();
        Task<ClientResponseDto?> GetByIdAsync(int id);
        Task<ClientResponseDto> CreateAsync(CreateClientDto dto);
        Task<bool> UpdateAsync(int id, UpdateClientDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
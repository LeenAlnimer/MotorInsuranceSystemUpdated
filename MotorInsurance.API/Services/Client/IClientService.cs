using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Client;
using MotorInsurance.API.DTOs.QueryParams;

namespace MotorInsurance.API.Services.Client
{
    public interface IClientService
    {
        Task<PagedResult<ClientResponseDto>> GetPagedAsync(ClientQueryParams queryParams);
        Task<ClientResponseDto?> GetByIdAsync(int id);
        Task<ClientResponseDto> CreateAsync(CreateClientDto dto);
        Task<bool> UpdateAsync(int id, UpdateClientDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

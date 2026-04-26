using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Car;
using MotorInsurance.API.DTOs.QueryParams;

namespace MotorInsurance.API.Services.Car
{
    public interface ICarService
    {
        Task<PagedResult<CarResponseDto>> GetPagedAsync(CarQueryParams queryParams);
        Task<PagedResult<CarResponseDto>> GetPagedByClientIdAsync(int clientId, CarQueryParams queryParams);
        Task<CarResponseDto?> GetByIdAsync(int id);
        Task<CarResponseDto> CreateAsync(CreateCarDto dto);
        Task<bool> UpdateAsync(int id, UpdateCarDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

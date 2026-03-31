using MotorInsurance.API.DTOs.Car;

namespace MotorInsurance.API.Services.Car
{
    public interface ICarService
    {
        Task<List<CarResponseDto>> GetAllAsync();
        Task<CarResponseDto?> GetByIdAsync(int id);
        Task<CarResponseDto> CreateAsync(CreateCarDto dto);
        Task<bool> UpdateAsync(int id, UpdateCarDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
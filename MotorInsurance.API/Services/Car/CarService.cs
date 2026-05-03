using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Car;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.Car;

namespace MotorInsurance.API.Services.Car
{
    public class CarService : ICarService
    {
        private readonly ICarRepository _repository;
        private readonly InsurancePricingSettings _pricing;

        public CarService(ICarRepository repository, IOptions<InsurancePricingSettings> pricing)
        {
            _repository = repository;
            _pricing = pricing.Value;
        }

        public async Task<PagedResult<CarResponseDto>> GetPagedAsync(CarQueryParams q)
        {
            return await BuildPagedResult(_repository.GetQueryable(), q);
        }

        public async Task<PagedResult<CarResponseDto>> GetPagedByUserIdAsync(int userId, CarQueryParams q)
        {
            var query = _repository.GetQueryable().Where(c => c.UserId == userId);
            return await BuildPagedResult(query, q);
        }

        public async Task<CarResponseDto?> GetByIdAsync(int id)
        {
            var car = await _repository.GetByIdAsync(id);
            return car == null ? null : MapToDto(car);
        }

        public async Task<CarResponseDto> CreateAsync(CreateCarDto dto, int userId)
        {
            if (DateTime.UtcNow.Year - dto.Year > _pricing.MaxCarAgeYears)
                throw new ArgumentException($"Car must be {_pricing.MaxCarAgeYears} years or newer");

            var car = new Models.Car
            {
                Brand = dto.Brand,
                Model = dto.Model,
                Year = dto.Year,
                Price = dto.Price,
                FuelType = dto.FuelType,
                UserId = userId
            };

            await _repository.AddAsync(car);
            await _repository.SaveChangesAsync();

            return MapToDto(car);
        }

        public async Task<bool> UpdateAsync(int id, UpdateCarDto dto, int userId)
        {
            var car = await _repository.GetByIdAsync(id);
            if (car == null) return false;

            if (car.UserId != userId)
                throw new UnauthorizedAccessException("Car does not belong to this user");

            if (dto.Brand != null)       car.Brand    = dto.Brand;
            if (dto.Model != null)       car.Model    = dto.Model;
            if (dto.Year.HasValue)
            {
                if (DateTime.UtcNow.Year - dto.Year.Value > _pricing.MaxCarAgeYears)
                    throw new ArgumentException($"Car must be {_pricing.MaxCarAgeYears} years or newer");
                car.Year = dto.Year.Value;
            }
            if (dto.Price.HasValue)      car.Price    = dto.Price.Value;
            if (dto.FuelType.HasValue)   car.FuelType = dto.FuelType.Value;

            _repository.Update(car);
            await _repository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var car = await _repository.GetByIdAsync(id);
            if (car == null) return false;

            if (car.UserId != userId)
                throw new UnauthorizedAccessException("Car does not belong to this user");

            _repository.Delete(car);
            await _repository.SaveChangesAsync();

            return true;
        }

        private async Task<PagedResult<CarResponseDto>> BuildPagedResult(
            IQueryable<Models.Car> query, CarQueryParams q)
        {
            if (!string.IsNullOrWhiteSpace(q.Brand))
                query = query.Where(c => c.Brand != null && c.Brand.ToLower().Contains(q.Brand.ToLower()));

            if (q.FuelType.HasValue)
                query = query.Where(c => c.FuelType == q.FuelType.Value);

            if (q.MinYear.HasValue)
                query = query.Where(c => c.Year >= q.MinYear.Value);

            if (q.MaxYear.HasValue)
                query = query.Where(c => c.Year <= q.MaxYear.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.Brand)
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .ToListAsync();

            return new PagedResult<CarResponseDto>
            {
                Data = items.Select(MapToDto).ToList(),
                Page = q.Page,
                PageSize = q.PageSize,
                TotalCount = totalCount
            };
        }

        private static CarResponseDto MapToDto(Models.Car c) => new CarResponseDto
        {
            Id = c.Id,
            Brand = c.Brand,
            Model = c.Model,
            Year = c.Year,
            Price = c.Price,
            FuelType = c.FuelType,
            UserId = c.UserId
        };
    }
}

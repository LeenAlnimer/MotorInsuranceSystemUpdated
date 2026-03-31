using MotorInsurance.API.DTOs.Car;
using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.Car;

namespace MotorInsurance.API.Services.Car
{
    public class CarService : ICarService 
    {
        private readonly ICarRepository _repository;

        public CarService(ICarRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CarResponseDto>> GetAllAsync()
        {
            var cars = await _repository.GetAllAsync();

            return cars.Select(c => new CarResponseDto
            {
                Id = c.Id,
                Brand = c.Brand,
                Model = c.Model,
                Year = c.Year,
                Price = c.Price
            }).ToList();
        }

        public async Task<CarResponseDto?> GetByIdAsync(int id)
        {
            var car = await _repository.GetByIdAsync(id);

            if (car == null) return null;

            return new CarResponseDto
            {
                Id = car.Id,
                Brand = car.Brand,
                Model = car.Model,
                Year = car.Year,
                Price = car.Price
            };
        }

        public async Task<CarResponseDto> CreateAsync(CreateCarDto dto)
        {
            
            var currentYear = DateTime.Now.Year;

            if (currentYear - dto.Year > 10)
                throw new Exception("Car must be 10 years or newer");

            var car = new Models.Car
            {
                Brand = dto.Brand,
                Model = dto.Model,
                Year = dto.Year,
                Price = dto.Price,
                FuelType = dto.FuelType,
                ClientId = dto.ClientId
            };

            await _repository.AddAsync(car);
            await _repository.SaveChangesAsync();

            return new CarResponseDto
            {
                Id = car.Id,
                Brand = car.Brand,
                Model = car.Model,
                Year = car.Year,
                Price = car.Price
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateCarDto dto)
        {
            var car = await _repository.GetByIdAsync(id);

            if (car == null) return false;

            car.Brand = dto.Brand;
            car.Model = dto.Model;
            car.Year = dto.Year;
            car.Price = dto.Price;
            car.FuelType = dto.FuelType;
            car.ClientId = dto.ClientId;

            _repository.Update(car);
            await _repository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var car = await _repository.GetByIdAsync(id);

            if (car == null) return false;

            _repository.Delete(car);
            await _repository.SaveChangesAsync();

            return true;
        }
    }
}
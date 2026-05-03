using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.Car;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.Car;
using MotorInsurance.API.Services.Car;

namespace MotorInsurance.Tests.Services
{
    public class CarServiceTests
    {
        private ApplicationDbContext CreateDb(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new ApplicationDbContext(options);
        }

        private CarService CreateService(ApplicationDbContext db)
        {
            var repo = new Mock<ICarRepository>();
            repo.Setup(r => r.GetQueryable()).Returns(db.Cars.AsQueryable());
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .Returns<int>(id => db.Cars.FirstOrDefaultAsync(c => c.Id == id));
            repo.Setup(r => r.AddAsync(It.IsAny<Car>()))
                .Callback<Car>(car => db.Cars.Add(car))
                .Returns(Task.CompletedTask);
            repo.Setup(r => r.SaveChangesAsync())
                .Returns(() => db.SaveChangesAsync().ContinueWith(_ => { }));
            repo.Setup(r => r.Update(It.IsAny<Car>()))
                .Callback<Car>(car => db.Cars.Update(car));
            repo.Setup(r => r.Delete(It.IsAny<Car>()))
                .Callback<Car>(car => { car.IsDeleted = true; car.DeletedAt = DateTime.UtcNow; });
            var pricing = Options.Create(new InsurancePricingSettings());
            return new CarService(repo.Object, pricing);
        }

        [Fact]
        public async Task CreateAsync_TooOldCar_ThrowsArgumentException()
        {
            using var db = CreateDb(nameof(CreateAsync_TooOldCar_ThrowsArgumentException));
            var service = CreateService(db);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateAsync(new CreateCarDto
                {
                    Brand = "Toyota", Model = "Camry",
                    Year = DateTime.UtcNow.Year - 11,
                    Price = 10000, FuelType = FuelType.Petrol
                }, userId: 1));
        }

        [Fact]
        public async Task CreateAsync_ValidCar_ReturnsDto()
        {
            using var db = CreateDb(nameof(CreateAsync_ValidCar_ReturnsDto));
            var service = CreateService(db);

            var result = await service.CreateAsync(new CreateCarDto
            {
                Brand = "Toyota", Model = "Camry",
                Year = DateTime.UtcNow.Year - 2,
                Price = 15000, FuelType = FuelType.Petrol
            }, userId: 1);

            Assert.Equal("Toyota", result.Brand);
            Assert.Equal(15000m, result.Price);
        }

        [Fact]
        public async Task GetPagedByUserIdAsync_ReturnsOnlyUserCars()
        {
            using var db = CreateDb(nameof(GetPagedByUserIdAsync_ReturnsOnlyUserCars));
            db.Cars.AddRange(
                new Car { Id = 1, Brand = "Toyota", Model = "Camry", Year = 2022, Price = 10000, FuelType = FuelType.Petrol, UserId = 1 },
                new Car { Id = 2, Brand = "Honda",  Model = "Civic",  Year = 2021, Price = 9000,  FuelType = FuelType.Petrol, UserId = 2 },
                new Car { Id = 3, Brand = "Kia",    Model = "Sportage", Year = 2020, Price = 8000, FuelType = FuelType.Diesel, UserId = 1 }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.GetPagedByUserIdAsync(1, new CarQueryParams { Page = 1, PageSize = 10 });

            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Data, c => Assert.Equal(1, c.UserId));
        }

        [Fact]
        public async Task GetPagedAsync_FilterByFuelType_ReturnsMatchingCars()
        {
            using var db = CreateDb(nameof(GetPagedAsync_FilterByFuelType_ReturnsMatchingCars));
            db.Cars.AddRange(
                new Car { Id = 1, Brand = "Toyota", Model = "Camry",   Year = 2022, Price = 10000, FuelType = FuelType.Electric, UserId = 1 },
                new Car { Id = 2, Brand = "Honda",  Model = "Civic",   Year = 2021, Price = 9000,  FuelType = FuelType.Petrol,   UserId = 1 },
                new Car { Id = 3, Brand = "Nissan", Model = "Leaf",    Year = 2023, Price = 12000, FuelType = FuelType.Electric, UserId = 1 }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.GetPagedAsync(new CarQueryParams { FuelType = FuelType.Electric, Page = 1, PageSize = 10 });

            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Data, c => Assert.Equal(FuelType.Electric, c.FuelType));
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesCar()
        {
            using var db = CreateDb(nameof(DeleteAsync_SoftDeletesCar));
            db.Cars.Add(new Car { Id = 1, Brand = "Toyota", Model = "Camry", Year = 2022, Price = 10000, FuelType = FuelType.Petrol, UserId = 1 });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.DeleteAsync(1, userId: 1);

            Assert.True(result);
            var car = await db.Cars.IgnoreQueryFilters().FirstAsync(c => c.Id == 1);
            Assert.True(car.IsDeleted);
            Assert.NotNull(car.DeletedAt);
        }

        [Fact]
        public async Task GetPagedAsync_DoesNotReturnDeletedCars()
        {
            using var db = CreateDb(nameof(GetPagedAsync_DoesNotReturnDeletedCars));
            db.Cars.AddRange(
                new Car { Id = 1, Brand = "Toyota", Model = "Camry", Year = 2022, Price = 10000, FuelType = FuelType.Petrol, UserId = 1, IsDeleted = false },
                new Car { Id = 2, Brand = "Honda",  Model = "Civic", Year = 2021, Price = 9000,  FuelType = FuelType.Petrol, UserId = 1, IsDeleted = true  }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.GetPagedAsync(new CarQueryParams { Page = 1, PageSize = 10 });

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Toyota", result.Data[0].Brand);
        }

        [Fact]
        public async Task UpdateAsync_CarNotFound_ReturnsFalse()
        {
            using var db = CreateDb(nameof(UpdateAsync_CarNotFound_ReturnsFalse));
            var service = CreateService(db);

            var result = await service.UpdateAsync(999, new UpdateCarDto
            {
                Brand = "X", Model = "Y", Year = 2022,
                Price = 5000, FuelType = FuelType.Petrol
            }, userId: 1);

            Assert.False(result);
        }
    }
}

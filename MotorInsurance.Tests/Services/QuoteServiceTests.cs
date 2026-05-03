using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.Quote;
using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.Quote;
using MotorInsurance.API.Services.Quote;

namespace MotorInsurance.Tests.Services
{
    public class QuoteServiceTests
    {
        private readonly Mock<IQuoteRepository> _quoteRepo = new();
        private readonly Mock<ILogger<QuoteService>> _logger = new();

        private ApplicationDbContext CreateDb(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ApplicationDbContext(options);
        }

        private QuoteService CreateService(ApplicationDbContext db) =>
            new(_quoteRepo.Object, db, Options.Create(new InsurancePricingSettings()), _logger.Object);

        private Car MakeCar(int year, decimal price, FuelType fuelType = FuelType.Petrol) => new()
        {
            Id = 1,
            Brand = "Toyota",
            Model = "Camry",
            Year = year,
            Price = price,
            FuelType = fuelType,
            UserId = 1
        };

        [Fact]
        public async Task CreateAsync_NewCar_AppliesNewCarMultiplier()
        {
            using var db = CreateDb(nameof(CreateAsync_NewCar_AppliesNewCarMultiplier));
            db.Cars.Add(MakeCar(DateTime.UtcNow.Year - 1, 20_000m));
            await db.SaveChangesAsync();

            _quoteRepo.Setup(r => r.AddAsync(It.IsAny<Quote>())).Returns(Task.CompletedTask);
            _quoteRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await CreateService(db).CreateAsync(new CreateQuoteDto { CarId = 1 });

            // 20000 * 0.05 * 1.2 = 1200
            Assert.Equal(1200m, result.Price);
        }

        [Fact]
        public async Task CreateAsync_OldCar_AppliesOldCarMultiplier()
        {
            using var db = CreateDb(nameof(CreateAsync_OldCar_AppliesOldCarMultiplier));
            db.Cars.Add(MakeCar(DateTime.UtcNow.Year - 9, 20_000m));
            await db.SaveChangesAsync();

            _quoteRepo.Setup(r => r.AddAsync(It.IsAny<Quote>())).Returns(Task.CompletedTask);
            _quoteRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await CreateService(db).CreateAsync(new CreateQuoteDto { CarId = 1 });

            // 20000 * 0.05 * 0.9 = 900
            Assert.Equal(900m, result.Price);
        }

        [Fact]
        public async Task CreateAsync_ElectricCar_AppliesElectricDiscount()
        {
            using var db = CreateDb(nameof(CreateAsync_ElectricCar_AppliesElectricDiscount));
            db.Cars.Add(MakeCar(DateTime.UtcNow.Year - 5, 20_000m, FuelType.Electric));
            await db.SaveChangesAsync();

            _quoteRepo.Setup(r => r.AddAsync(It.IsAny<Quote>())).Returns(Task.CompletedTask);
            _quoteRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await CreateService(db).CreateAsync(new CreateQuoteDto { CarId = 1 });

            // 20000 * 0.05 * 0.9 = 900
            Assert.Equal(900m, result.Price);
        }

        [Fact]
        public async Task CreateAsync_DieselCar_AppliesDieselSurcharge()
        {
            using var db = CreateDb(nameof(CreateAsync_DieselCar_AppliesDieselSurcharge));
            db.Cars.Add(MakeCar(DateTime.UtcNow.Year - 5, 20_000m, FuelType.Diesel));
            await db.SaveChangesAsync();

            _quoteRepo.Setup(r => r.AddAsync(It.IsAny<Quote>())).Returns(Task.CompletedTask);
            _quoteRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await CreateService(db).CreateAsync(new CreateQuoteDto { CarId = 1 });

            // 20000 * 0.05 * 1.1 = 1100
            Assert.Equal(1100m, result.Price);
        }

        [Fact]
        public async Task CreateAsync_CheapCar_EnforcesMinimumPremium()
        {
            using var db = CreateDb(nameof(CreateAsync_CheapCar_EnforcesMinimumPremium));
            db.Cars.Add(MakeCar(DateTime.UtcNow.Year - 5, 1_000m));
            await db.SaveChangesAsync();

            _quoteRepo.Setup(r => r.AddAsync(It.IsAny<Quote>())).Returns(Task.CompletedTask);
            _quoteRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await CreateService(db).CreateAsync(new CreateQuoteDto { CarId = 1 });

            // 1000 * 0.05 * 0.95 = 47.5 → minimum 300
            Assert.Equal(300m, result.Price);
        }

        [Fact]
        public async Task CreateAsync_TooOldCar_ThrowsArgumentException()
        {
            using var db = CreateDb(nameof(CreateAsync_TooOldCar_ThrowsArgumentException));
            db.Cars.Add(MakeCar(DateTime.UtcNow.Year - 11, 20_000m));
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService(db).CreateAsync(new CreateQuoteDto { CarId = 1 }));
        }

        [Fact]
        public async Task CreateAsync_CarNotFound_ThrowsArgumentException()
        {
            using var db = CreateDb(nameof(CreateAsync_CarNotFound_ThrowsArgumentException));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService(db).CreateAsync(new CreateQuoteDto { CarId = 999 }));
        }

        [Fact]
        public async Task CreateAsync_ClientDoesNotOwnCar_ThrowsUnauthorizedAccessException()
        {
            using var db = CreateDb(nameof(CreateAsync_ClientDoesNotOwnCar_ThrowsUnauthorizedAccessException));
            var car = MakeCar(DateTime.UtcNow.Year - 1, 20_000m);
            car.UserId = 5;
            db.Cars.Add(car);
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                CreateService(db).CreateAsync(new CreateQuoteDto { CarId = 1 }, userId: 99));
        }

        [Fact]
        public async Task ApproveQuoteAsync_AlreadyApproved_ThrowsArgumentException()
        {
            using var db = CreateDb(nameof(ApproveQuoteAsync_AlreadyApproved_ThrowsArgumentException));
            var car = new Car { Id = 1, Brand = "Toyota", Model = "Camry", Year = 2022, Price = 20_000m, FuelType = FuelType.Petrol, UserId = 1 };
            var quote = new Quote
            {
                Id = 1, CarId = 1, Price = 1000m,
                Status = QuoteStatus.Approved,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Car = car
            };
            db.Cars.Add(car);
            db.Quotes.Add(quote);
            await db.SaveChangesAsync();

            _quoteRepo.Setup(r => r.GetByIdWithCarAsync(1)).ReturnsAsync(quote);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService(db).ApproveQuoteAsync(1));
        }

        [Fact]
        public async Task ApproveQuoteAsync_ValidQuote_CreatesPolicy()
        {
            using var db = CreateDb(nameof(ApproveQuoteAsync_ValidQuote_CreatesPolicy));
            var car = new Car { Id = 1, Brand = "Toyota", Model = "Camry", Year = 2022, Price = 20_000m, FuelType = FuelType.Petrol, UserId = 1 };
            var quote = new Quote
            {
                Id = 1, CarId = 1, Price = 1000m,
                Status = QuoteStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Car = car
            };
            db.Cars.Add(car);
            db.Quotes.Add(quote);
            await db.SaveChangesAsync();

            _quoteRepo.Setup(r => r.GetByIdWithCarAsync(1)).ReturnsAsync(quote);

            var result = await CreateService(db).ApproveQuoteAsync(1);

            Assert.True(result);
            var updatedQuote = await db.Quotes.FindAsync(1);
            Assert.Equal(QuoteStatus.Approved, updatedQuote!.Status);
        }
    }
}

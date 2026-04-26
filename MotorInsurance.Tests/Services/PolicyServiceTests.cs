using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Models;
using MotorInsurance.API.Services.Policy;

namespace MotorInsurance.Tests.Services
{
    public class PolicyServiceTests
    {
        private readonly Mock<ILogger<PolicyService>> _logger = new();

        private ApplicationDbContext CreateDb(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new ApplicationDbContext(options);
        }

        private PolicyService CreateService(ApplicationDbContext db) =>
            new(db, _logger.Object);

        private async Task<ApplicationDbContext> SeedAsync(string dbName)
        {
            var db = CreateDb(dbName);

            var client1 = new Client { Id = 1, FullName = "Ahmad Ali",  Email = "a@a.com", PhoneNumber = "0791111111" };
            var client2 = new Client { Id = 2, FullName = "Sara Hassan", Email = "s@s.com", PhoneNumber = "0792222222" };

            var car1 = new Car { Id = 1, Brand = "Toyota", Model = "Camry",  Year = 2022, Price = 20_000m, FuelType = FuelType.Petrol, ClientId = 1 };
            var car2 = new Car { Id = 2, Brand = "Honda",  Model = "Civic",  Year = 2021, Price = 15_000m, FuelType = FuelType.Petrol, ClientId = 2 };

            var quote1 = new Quote { Id = 1, CarId = 1, Price = 1000m, IsApproved = true,  CreatedAt = DateTime.UtcNow };
            var quote2 = new Quote { Id = 2, CarId = 2, Price = 750m,  IsApproved = true,  CreatedAt = DateTime.UtcNow };

            var policy1 = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(355) };
            var policy2 = new Policy { Id = 2, QuoteId = 2, StartDate = DateTime.UtcNow.AddDays(-5),  EndDate = DateTime.UtcNow.AddDays(360) };

            db.Clients.AddRange(client1, client2);
            db.Cars.AddRange(car1, car2);
            db.Quotes.AddRange(quote1, quote2);
            db.Policies.AddRange(policy1, policy2);
            await db.SaveChangesAsync();

            return db;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingPolicy_ReturnsCorrectDto()
        {
            using var db = await SeedAsync(nameof(GetByIdAsync_ExistingPolicy_ReturnsCorrectDto));
            var service = CreateService(db);

            var result = await service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(1, result.QuoteId);
            Assert.Equal(1000m, result.Price);
            Assert.Equal("Ahmad Ali", result.ClientName);
            Assert.Equal("Toyota", result.CarBrand);
            Assert.Equal("Camry", result.CarModel);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentPolicy_ReturnsNull()
        {
            using var db = CreateDb(nameof(GetByIdAsync_NonExistentPolicy_ReturnsNull));
            var service = CreateService(db);

            var result = await service.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPagedAsync_ReturnsAllPolicies()
        {
            using var db = await SeedAsync(nameof(GetPagedAsync_ReturnsAllPolicies));
            var service = CreateService(db);

            var result = await service.GetPagedAsync(new PolicyQueryParams { Page = 1, PageSize = 10 });

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Data.Count);
        }

        [Fact]
        public async Task GetPagedByClientIdAsync_ReturnsOnlyClientPolicies()
        {
            using var db = await SeedAsync(nameof(GetPagedByClientIdAsync_ReturnsOnlyClientPolicies));
            var service = CreateService(db);

            var result = await service.GetPagedByClientIdAsync(1, new PolicyQueryParams { Page = 1, PageSize = 10 });

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Ahmad Ali", result.Data[0].ClientName);
        }

        [Fact]
        public async Task GetPagedAsync_SortByEndDateDesc_ReturnsSortedPolicies()
        {
            using var db = await SeedAsync(nameof(GetPagedAsync_SortByEndDateDesc_ReturnsSortedPolicies));
            var service = CreateService(db);

            var result = await service.GetPagedAsync(new PolicyQueryParams
            {
                Page = 1, PageSize = 10,
                SortBy = "enddate",
                SortOrder = "desc"
            });

            Assert.Equal(2, result.TotalCount);
            Assert.True(result.Data[0].EndDate >= result.Data[1].EndDate);
        }

        [Fact]
        public async Task GetPagedAsync_SortByStartDateAsc_ReturnsSortedPolicies()
        {
            using var db = await SeedAsync(nameof(GetPagedAsync_SortByStartDateAsc_ReturnsSortedPolicies));
            var service = CreateService(db);

            var result = await service.GetPagedAsync(new PolicyQueryParams
            {
                Page = 1, PageSize = 10,
                SortBy = "startdate",
                SortOrder = "asc"
            });

            Assert.Equal(2, result.TotalCount);
            Assert.True(result.Data[0].StartDate <= result.Data[1].StartDate);
        }

        [Fact]
        public async Task GetPagedAsync_Pagination_ReturnsCorrectPage()
        {
            using var db = await SeedAsync(nameof(GetPagedAsync_Pagination_ReturnsCorrectPage));
            var service = CreateService(db);

            var result = await service.GetPagedAsync(new PolicyQueryParams { Page = 1, PageSize = 1 });

            Assert.Equal(2, result.TotalCount);
            Assert.Single(result.Data);
            Assert.Equal(1, result.Page);
            Assert.Equal(1, result.PageSize);
        }

        [Fact]
        public async Task GetPagedByClientIdAsync_OtherClientHasNoPolicies_ReturnsEmpty()
        {
            using var db = await SeedAsync(nameof(GetPagedByClientIdAsync_OtherClientHasNoPolicies_ReturnsEmpty));
            var service = CreateService(db);

            var result = await service.GetPagedByClientIdAsync(999, new PolicyQueryParams { Page = 1, PageSize = 10 });

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Data);
        }
    }
}

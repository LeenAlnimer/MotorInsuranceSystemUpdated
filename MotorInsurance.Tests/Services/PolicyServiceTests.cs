using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.Policy;
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

        private static readonly IOptions<InsurancePricingSettings> _pricingOptions =
            Options.Create(new InsurancePricingSettings());

        private PolicyService CreateService(ApplicationDbContext db) =>
            new(new PolicyRepository(db), db, _pricingOptions, _logger.Object);

        private async Task<ApplicationDbContext> SeedAsync(string dbName)
        {
            var db = CreateDb(dbName);

            var user1 = new User { Id = 1, Username = "Ahmad Ali",   Email = "a@a.com", PhoneNumber = "0791111111", PasswordHash = "x" };
            var user2 = new User { Id = 2, Username = "Sara Hassan",  Email = "s@s.com", PhoneNumber = "0792222222", PasswordHash = "x" };

            var car1 = new Car { Id = 1, Brand = "Toyota", Model = "Camry",  Year = 2022, Price = 20_000m, FuelType = FuelType.Petrol, UserId = 1 };
            var car2 = new Car { Id = 2, Brand = "Honda",  Model = "Civic",  Year = 2021, Price = 15_000m, FuelType = FuelType.Petrol, UserId = 2 };

            var quote1 = new Quote { Id = 1, CarId = 1, Price = 1000m, Status = QuoteStatus.Approved, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };
            var quote2 = new Quote { Id = 2, CarId = 2, Price = 750m,  Status = QuoteStatus.Approved, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };

            var policy1 = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(355) };
            var policy2 = new Policy { Id = 2, QuoteId = 2, StartDate = DateTime.UtcNow.AddDays(-5),  EndDate = DateTime.UtcNow.AddDays(360) };

            db.Users.AddRange(user1, user2);
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
            Assert.Equal("Ahmad Ali", result.UserName);
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
        public async Task GetPagedByUserIdAsync_ReturnsOnlyUserPolicies()
        {
            using var db = await SeedAsync(nameof(GetPagedByUserIdAsync_ReturnsOnlyUserPolicies));
            var service = CreateService(db);

            var result = await service.GetPagedByUserIdAsync(1, new PolicyQueryParams { Page = 1, PageSize = 10 });

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Ahmad Ali", result.Data[0].UserName);
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
        public async Task GetPagedByUserIdAsync_OtherUserHasNoPolicies_ReturnsEmpty()
        {
            using var db = await SeedAsync(nameof(GetPagedByUserIdAsync_OtherUserHasNoPolicies_ReturnsEmpty));
            var service = CreateService(db);

            var result = await service.GetPagedByUserIdAsync(999, new PolicyQueryParams { Page = 1, PageSize = 10 });

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Data);
        }

        // ── RenewAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task RenewAsync_ActiveNotExpiredPolicy_ThrowsInvalidOperation()
        {
            using var db = await SeedAsync(nameof(RenewAsync_ActiveNotExpiredPolicy_ThrowsInvalidOperation));
            var service = CreateService(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RenewAsync(1));
        }

        [Fact]
        public async Task RenewAsync_ExpiredPolicy_ReturnsNewActivePolicy()
        {
            using var db = CreateDb(nameof(RenewAsync_ExpiredPolicy_ReturnsNewActivePolicy));
            var user = new User { Id = 1, Username = "Ahmad Ali", Email = "a@a.com", PhoneNumber = "0791111111", PasswordHash = "x" };
            var car = new Car { Id = 1, Brand = "Toyota", Model = "Camry", Year = 2022, Price = 20_000m, FuelType = FuelType.Petrol, UserId = 1 };
            var quote = new Quote { Id = 1, CarId = 1, Price = 1000m, Status = QuoteStatus.Approved, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };
            var expired = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddYears(-2), EndDate = DateTime.UtcNow.AddDays(-1), Status = PolicyStatus.Expired };
            db.Users.Add(user); db.Cars.Add(car); db.Quotes.Add(quote); db.Policies.Add(expired);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.RenewAsync(1);

            Assert.NotEqual(1, result.Id);
            Assert.Equal(PolicyStatus.Active, result.Status);
            Assert.True(result.EndDate > DateTime.UtcNow);
        }

        [Fact]
        public async Task RenewAsync_NonExistentPolicy_ThrowsKeyNotFound()
        {
            using var db = CreateDb(nameof(RenewAsync_NonExistentPolicy_ThrowsKeyNotFound));
            var service = CreateService(db);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RenewAsync(999));
        }

        // ── CancelAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task CancelAsync_ActivePolicy_ReturnsCancelledPolicy()
        {
            using var db = await SeedAsync(nameof(CancelAsync_ActivePolicy_ReturnsCancelledPolicy));
            var service = CreateService(db);

            var result = await service.CancelAsync(1, performedByUserId: 99);

            Assert.Equal(PolicyStatus.Cancelled, result.Status);
        }

        [Fact]
        public async Task CancelAsync_NonExistentPolicy_ThrowsKeyNotFound()
        {
            using var db = CreateDb(nameof(CancelAsync_NonExistentPolicy_ThrowsKeyNotFound));
            var service = CreateService(db);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CancelAsync(999, performedByUserId: 99));
        }

        [Fact]
        public async Task CancelAsync_AlreadyCancelledPolicy_ThrowsInvalidOperation()
        {
            using var db = CreateDb(nameof(CancelAsync_AlreadyCancelledPolicy_ThrowsInvalidOperation));
            var user  = new User { Id = 1, Username = "u", Email = "u@u.com", PhoneNumber = "0791234567", PasswordHash = "x" };
            var car   = new Car { Id = 1, Brand = "B", Model = "M", Year = 2020, Price = 10000, FuelType = FuelType.Petrol, UserId = 1 };
            var quote = new Quote { Id = 1, CarId = 1, Price = 500m, Status = QuoteStatus.Approved, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };
            var policy = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(355), Status = PolicyStatus.Cancelled };
            db.Users.Add(user); db.Cars.Add(car); db.Quotes.Add(quote); db.Policies.Add(policy);
            await db.SaveChangesAsync();

            var service = CreateService(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelAsync(1, performedByUserId: 99));
        }

        [Fact]
        public async Task CancelAsync_ExpiredPolicy_ThrowsInvalidOperation()
        {
            using var db = CreateDb(nameof(CancelAsync_ExpiredPolicy_ThrowsInvalidOperation));
            var user  = new User { Id = 1, Username = "u", Email = "u@u.com", PhoneNumber = "0791234567", PasswordHash = "x" };
            var car   = new Car { Id = 1, Brand = "B", Model = "M", Year = 2020, Price = 10000, FuelType = FuelType.Petrol, UserId = 1 };
            var quote = new Quote { Id = 1, CarId = 1, Price = 500m, Status = QuoteStatus.Approved, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };
            var policy = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddYears(-2), EndDate = DateTime.UtcNow.AddDays(-1), Status = PolicyStatus.Expired };
            db.Users.Add(user); db.Cars.Add(car); db.Quotes.Add(quote); db.Policies.Add(policy);
            await db.SaveChangesAsync();

            var service = CreateService(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelAsync(1, performedByUserId: 99));
        }

        [Fact]
        public async Task RenewAsync_CancelledPolicy_ThrowsInvalidOperation()
        {
            using var db = CreateDb(nameof(RenewAsync_CancelledPolicy_ThrowsInvalidOperation));
            var user  = new User { Id = 1, Username = "u", Email = "u@u.com", PhoneNumber = "0791234567", PasswordHash = "x" };
            var car   = new Car { Id = 1, Brand = "B", Model = "M", Year = 2020, Price = 10000, FuelType = FuelType.Petrol, UserId = 1 };
            var quote = new Quote { Id = 1, CarId = 1, Price = 500m, Status = QuoteStatus.Approved, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };
            var policy = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(335), Status = PolicyStatus.Cancelled };
            db.Users.Add(user); db.Cars.Add(car); db.Quotes.Add(quote); db.Policies.Add(policy);
            await db.SaveChangesAsync();

            var service = CreateService(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RenewAsync(1));
        }
    }
}

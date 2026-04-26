using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.Claim;
using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.Claim;
using MotorInsurance.API.Services.Claim;
using MotorInsurance.API.Services.Email;

namespace MotorInsurance.Tests.Services
{
    public class ClaimServiceTests
    {
        private readonly Mock<ILogger<ClaimService>> _logger = new();
        private readonly Mock<IEmailService> _emailService = new();

        private ApplicationDbContext CreateDb(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new ApplicationDbContext(options);
        }

        private ClaimService CreateService(ApplicationDbContext db)
        {
            var repo = new Mock<IClaimRepository>();
            repo.Setup(r => r.PolicyExists(It.IsAny<int>())).ReturnsAsync(true);
            repo.Setup(r => r.UserExists(It.IsAny<int>())).ReturnsAsync(true);
            repo.Setup(r => r.AddAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);
            repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _emailService.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            return new ClaimService(repo.Object, db, _emailService.Object, _logger.Object);
        }

        private async Task<ApplicationDbContext> SeedActivePolicyAsync(string dbName)
        {
            var db = CreateDb(dbName);
            var user = new User { Id = 1, Username = "u", Email = "u@u.com", PhoneNumber = "0791234567", PasswordHash = "x" };
            var client = new Client { Id = 1, UserId = 1 };
            var car = new Car { Id = 1, ClientId = 1, Brand = "B", Model = "M", Year = 2020, Price = 10000, FuelType = FuelType.Petrol };
            var quote = new Quote { Id = 1, CarId = 1, Price = 500, IsApproved = true, CreatedAt = DateTime.UtcNow };
            var policy = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddYears(1) };
            db.Users.Add(user);
            db.Clients.Add(client);
            db.Cars.Add(car);
            db.Quotes.Add(quote);
            db.Policies.Add(policy);
            await db.SaveChangesAsync();
            return db;
        }

        [Fact]
        public async Task CreateAsync_PolicyNotFound_ReturnsFailure()
        {
            using var db = CreateDb(nameof(CreateAsync_PolicyNotFound_ReturnsFailure));
            var service = CreateService(db);

            var (success, message, _) = await service.CreateAsync(new CreateClaimDto { PolicyId = 999, Description = "test" }, 1);

            Assert.False(success);
            Assert.Equal("Policy not found", message);
        }

        [Fact]
        public async Task CreateAsync_ExpiredPolicy_ReturnsFailure()
        {
            using var db = CreateDb(nameof(CreateAsync_ExpiredPolicy_ReturnsFailure));
            var policy = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddYears(-2), EndDate = DateTime.UtcNow.AddDays(-1) };
            db.Policies.Add(policy);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var (success, message, _) = await service.CreateAsync(new CreateClaimDto { PolicyId = 1, Description = "test" }, 1);

            Assert.False(success);
            Assert.Equal("Policy has expired", message);
        }

        [Fact]
        public async Task CreateAsync_PolicyBelongsToOtherClient_ReturnsFailure()
        {
            using var db = await SeedActivePolicyAsync(nameof(CreateAsync_PolicyBelongsToOtherClient_ReturnsFailure));

            // Add a second client linked to userId=2
            db.Clients.Add(new Client { Id = 2, UserId = 2 });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var (success, message, _) = await service.CreateAsync(new CreateClaimDto { PolicyId = 1, Description = "test" }, userId: 2);

            Assert.False(success);
            Assert.Equal("Policy does not belong to you", message);
        }

        [Fact]
        public async Task CreateAsync_ValidClaim_ReturnsSuccess()
        {
            using var db = await SeedActivePolicyAsync(nameof(CreateAsync_ValidClaim_ReturnsSuccess));
            var service = CreateService(db);

            var (success, _, claim) = await service.CreateAsync(new CreateClaimDto { PolicyId = 1, Description = "Accident" }, userId: 1);

            Assert.True(success);
            Assert.NotNull(claim);
            Assert.Equal("Pending", claim.Status);
        }
    }
}

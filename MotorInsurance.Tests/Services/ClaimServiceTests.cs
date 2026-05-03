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

        private ClaimService CreateServiceWithRealRepo(ApplicationDbContext db)
        {
            _emailService.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            return new ClaimService(new ClaimRepository(db), db, _emailService.Object, _logger.Object);
        }

        private async Task<ApplicationDbContext> SeedActivePolicyAsync(string dbName)
        {
            var db = CreateDb(dbName);
            var user = new User { Id = 1, Username = "u", Email = "u@u.com", PhoneNumber = "0791234567", PasswordHash = "x" };
            var car = new Car { Id = 1, UserId = 1, Brand = "B", Model = "M", Year = 2020, Price = 10000, FuelType = FuelType.Petrol };
            var quote = new Quote { Id = 1, CarId = 1, Price = 500, Status = QuoteStatus.Approved, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };
            var policy = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddYears(1) };
            db.Users.Add(user);
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
            // Quote and Car must be seeded — EF Core In-Memory treats Include as inner join when entity missing
            var car   = new Car { Id = 1, Brand = "B", Model = "M", Year = 2020, Price = 10000, FuelType = FuelType.Petrol, UserId = 1 };
            var quote = new Quote { Id = 1, CarId = 1, Price = 500, Status = QuoteStatus.Approved, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };
            var policy = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddYears(-2), EndDate = DateTime.UtcNow.AddDays(-1) };
            db.Cars.Add(car); db.Quotes.Add(quote); db.Policies.Add(policy);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var (success, message, _) = await service.CreateAsync(new CreateClaimDto { PolicyId = 1, Description = "test", ClaimAmount = 100 }, 1);

            Assert.False(success);
            Assert.Equal("Policy has expired", message);
        }

        [Fact]
        public async Task CreateAsync_PolicyBelongsToOtherUser_ReturnsFailure()
        {
            using var db = await SeedActivePolicyAsync(nameof(CreateAsync_PolicyBelongsToOtherUser_ReturnsFailure));

            // Policy belongs to userId=1; submit as userId=2
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
            Assert.Equal(ClaimStatus.Pending, claim.Status);
        }

        // ── ApproveAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task ApproveAsync_NonExistentClaim_ReturnsFalse()
        {
            using var db = CreateDb(nameof(ApproveAsync_NonExistentClaim_ReturnsFalse));
            var service = CreateServiceWithRealRepo(db);

            var result = await service.ApproveAsync(999, 1);

            Assert.False(result);
        }

        [Fact]
        public async Task ApproveAsync_PendingClaim_ApprovesSuccessfully()
        {
            using var db = CreateDb(nameof(ApproveAsync_PendingClaim_ApprovesSuccessfully));
            var user   = new User { Id = 1, Username = "u", Email = "u@u.com", PhoneNumber = "0791234567", PasswordHash = "x" };
            var policy = new Policy { Id = 1, QuoteId = 1, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddYears(1), Status = PolicyStatus.Active };
            var claim  = new Claim { Id = 1, Description = "test", Status = ClaimStatus.Pending, PolicyId = 1, UserId = 1, CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.Policies.Add(policy);
            db.Claims.Add(claim);
            await db.SaveChangesAsync();

            var service = CreateServiceWithRealRepo(db);
            var result = await service.ApproveAsync(1, performedByUserId: 2);

            Assert.True(result);
            var updated = await db.Claims.FindAsync(1);
            Assert.Equal(ClaimStatus.Approved, updated!.Status);
            Assert.Equal(2, updated.ApprovedById);
            Assert.NotNull(updated.ApprovedAt);
        }

        [Fact]
        public async Task ApproveAsync_AlreadyApprovedClaim_ThrowsInvalidOperation()
        {
            using var db = CreateDb(nameof(ApproveAsync_AlreadyApprovedClaim_ThrowsInvalidOperation));
            var claim = new Claim { Id = 1, Description = "test", Status = ClaimStatus.Approved, PolicyId = 1, UserId = 1, CreatedAt = DateTime.UtcNow };
            db.Claims.Add(claim);
            await db.SaveChangesAsync();

            var service = CreateServiceWithRealRepo(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(1, 2));
        }

        // ── RejectAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task RejectAsync_NonExistentClaim_ReturnsFalse()
        {
            using var db = CreateDb(nameof(RejectAsync_NonExistentClaim_ReturnsFalse));
            var service = CreateServiceWithRealRepo(db);

            var result = await service.RejectAsync(999, 1);

            Assert.False(result);
        }

        [Fact]
        public async Task RejectAsync_PendingClaim_RejectsSuccessfully()
        {
            using var db = CreateDb(nameof(RejectAsync_PendingClaim_RejectsSuccessfully));
            var user = new User { Id = 1, Username = "u", Email = "u@u.com", PhoneNumber = "0791234567", PasswordHash = "x" };
            var claim = new Claim { Id = 1, Description = "test", Status = ClaimStatus.Pending, PolicyId = 1, UserId = 1, CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.Claims.Add(claim);
            await db.SaveChangesAsync();

            var service = CreateServiceWithRealRepo(db);
            var result = await service.RejectAsync(1, performedByUserId: 2);

            Assert.True(result);
            var updated = await db.Claims.FindAsync(1);
            Assert.Equal(ClaimStatus.Rejected, updated!.Status);
            Assert.Equal(2, updated.RejectedById);
            Assert.NotNull(updated.RejectedAt);
        }

        [Fact]
        public async Task RejectAsync_AlreadyRejectedClaim_ThrowsInvalidOperation()
        {
            using var db = CreateDb(nameof(RejectAsync_AlreadyRejectedClaim_ThrowsInvalidOperation));
            var claim = new Claim { Id = 1, Description = "test", Status = ClaimStatus.Rejected, PolicyId = 1, UserId = 1, CreatedAt = DateTime.UtcNow };
            db.Claims.Add(claim);
            await db.SaveChangesAsync();

            var service = CreateServiceWithRealRepo(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RejectAsync(1, 2));
        }

        // ── DeleteAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ExistingClaim_DeletesSuccessfully()
        {
            using var db = CreateDb(nameof(DeleteAsync_ExistingClaim_DeletesSuccessfully));
            var claim = new Claim { Id = 1, Description = "test", Status = ClaimStatus.Rejected, PolicyId = 1, UserId = 1, CreatedAt = DateTime.UtcNow };
            db.Claims.Add(claim);
            await db.SaveChangesAsync();

            var service = CreateServiceWithRealRepo(db);
            var result = await service.DeleteAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_NonExistentClaim_ReturnsFalse()
        {
            using var db = CreateDb(nameof(DeleteAsync_NonExistentClaim_ReturnsFalse));
            var service = CreateServiceWithRealRepo(db);

            var result = await service.DeleteAsync(999);

            Assert.False(result);
        }
    }
}

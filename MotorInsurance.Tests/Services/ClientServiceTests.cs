using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.Client;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.Client;
using MotorInsurance.API.Services.Client;

namespace MotorInsurance.Tests.Services
{
    public class ClientServiceTests
    {
        private readonly Mock<ILogger<ClientService>> _logger = new();

        private ApplicationDbContext CreateDb(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new ApplicationDbContext(options);
        }

        private ClientService CreateService(ApplicationDbContext db)
        {
            var repo = new Mock<IClientRepository>();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .Returns<int>(id => db.Clients.Include(c => c.Cars).FirstOrDefaultAsync(c => c.Id == id));
            repo.Setup(r => r.AddAsync(It.IsAny<Client>()))
                .Callback<Client>(c => db.Clients.Add(c))
                .Returns(Task.CompletedTask);
            repo.Setup(r => r.SaveChangesAsync())
                .Returns(() => db.SaveChangesAsync().ContinueWith(_ => { }));
            repo.Setup(r => r.Update(It.IsAny<Client>()))
                .Callback<Client>(c => db.Clients.Update(c));
            repo.Setup(r => r.Delete(It.IsAny<Client>()))
                .Callback<Client>(c => { c.IsDeleted = true; c.DeletedAt = DateTime.UtcNow; });
            return new ClientService(repo.Object, db, _logger.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsClient()
        {
            using var db = CreateDb(nameof(CreateAsync_ValidDto_ReturnsClient));
            var service = CreateService(db);

            var result = await service.CreateAsync(new CreateClientDto
            {
                FullName = "Leen Alnimer",
                Email = "leen@test.com",
                PhoneNumber = "0791234567"
            });

            Assert.Equal("Leen Alnimer", result.FullName);
            Assert.Equal("leen@test.com", result.Email);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            using var db = CreateDb(nameof(GetByIdAsync_NotFound_ReturnsNull));
            var service = CreateService(db);

            var result = await service.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_OnlySentFieldsUpdated()
        {
            using var db = CreateDb(nameof(UpdateAsync_OnlySentFieldsUpdated));
            db.Clients.Add(new Client { Id = 1, FullName = "Old Name", Email = "old@test.com", PhoneNumber = "0791111111" });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            // بنبعت FullName بس — Email وPhoneNumber ما لازم يتغيروا
            await service.UpdateAsync(1, new UpdateClientDto { FullName = "New Name" });

            var client = await db.Clients.FindAsync(1);
            Assert.Equal("New Name", client!.FullName);
            Assert.Equal("old@test.com", client.Email);
            Assert.Equal("0791111111", client.PhoneNumber);
        }

        [Fact]
        public async Task UpdateAsync_ClientNotFound_ReturnsFalse()
        {
            using var db = CreateDb(nameof(UpdateAsync_ClientNotFound_ReturnsFalse));
            var service = CreateService(db);

            var result = await service.UpdateAsync(999, new UpdateClientDto { FullName = "X" });

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesClient()
        {
            using var db = CreateDb(nameof(DeleteAsync_SoftDeletesClient));
            db.Clients.Add(new Client { Id = 1, FullName = "Test Client" });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.DeleteAsync(1);

            Assert.True(result);
            var client = await db.Clients.IgnoreQueryFilters().FirstAsync(c => c.Id == 1);
            Assert.True(client.IsDeleted);
            Assert.NotNull(client.DeletedAt);
        }

        [Fact]
        public async Task GetPagedAsync_DoesNotReturnDeletedClients()
        {
            using var db = CreateDb(nameof(GetPagedAsync_DoesNotReturnDeletedClients));
            db.Clients.AddRange(
                new Client { Id = 1, FullName = "Active Client",  IsDeleted = false },
                new Client { Id = 2, FullName = "Deleted Client", IsDeleted = true  }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.GetPagedAsync(new ClientQueryParams { Page = 1, PageSize = 10 });

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Active Client", result.Data[0].FullName);
        }

        [Fact]
        public async Task GetPagedAsync_SearchByName_ReturnsMatchingClients()
        {
            using var db = CreateDb(nameof(GetPagedAsync_SearchByName_ReturnsMatchingClients));
            db.Clients.AddRange(
                new Client { Id = 1, FullName = "Ahmad Ali" },
                new Client { Id = 2, FullName = "Sara Hassan" },
                new Client { Id = 3, FullName = "Ahmad Salem" }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.GetPagedAsync(new ClientQueryParams { Search = "ahmad", Page = 1, PageSize = 10 });

            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Data, c => Assert.Contains("Ahmad", c.FullName));
        }
    }
}

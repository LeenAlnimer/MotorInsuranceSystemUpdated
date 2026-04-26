using Microsoft.Extensions.Logging;
using Moq;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.User;
using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.Client;
using MotorInsurance.API.Repositories.RefreshToken;
using MotorInsurance.API.Repositories.User;
using MotorInsurance.API.Services.Auth;
using MotorInsurance.API.Services.User;
using Microsoft.Extensions.Configuration;

namespace MotorInsurance.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IClientRepository> _clientRepo = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
        private readonly Mock<ILogger<UserService>> _logger = new();

        private UserService CreateService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "super-secret-key-for-testing-1234567890",
                    ["Jwt:Issuer"] = "test",
                    ["Jwt:Audience"] = "test"
                })
                .Build();

            var jwtService = new JwtService(config);
            return new UserService(_userRepo.Object, _clientRepo.Object, _refreshTokenRepo.Object, jwtService, _logger.Object);
        }

        [Fact]
        public async Task LoginAsync_InvalidCredentials_ThrowsArgumentException()
        {
            _userRepo.Setup(r => r.GetByIdentifierAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.LoginAsync(new LoginDto { Identifier = "nobody", Password = "wrong" }));
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsArgumentException()
        {
            var user = new User
            {
                Id = 1, Username = "john", Email = "j@j.com", PhoneNumber = "0791234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass1!"),
                Role = AppRoles.Client
            };
            _userRepo.Setup(r => r.GetByIdentifierAsync("john")).ReturnsAsync(user);
            _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.LoginAsync(new LoginDto { Identifier = "john", Password = "WrongPass1!" }));
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUser()
        {
            var user = new User
            {
                Id = 1, Username = "john", Email = "j@j.com", PhoneNumber = "0791234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass1!"),
                Role = AppRoles.Client
            };
            _userRepo.Setup(r => r.GetByIdentifierAsync("john")).ReturnsAsync(user);
            _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _clientRepo.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(new Client { Id = 10, UserId = 1 });
            _refreshTokenRepo.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
            _refreshTokenRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = CreateService();
            var (token, refreshToken, returnedUser) = await service.LoginAsync(new LoginDto { Identifier = "john", Password = "CorrectPass1!" });

            Assert.NotEmpty(token);
            Assert.NotEmpty(refreshToken);
            Assert.Equal("john", returnedUser.Username);
        }

        [Fact]
        public async Task UpdateAsync_SameEmailAsOwn_DoesNotThrow()
        {
            var user = new User { Id = 1, Username = "john", Email = "john@test.com", PhoneNumber = "0791234567", PasswordHash = "x", Role = AppRoles.Client };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepo.Setup(r => r.GetByEmailAsync("john@test.com")).ReturnsAsync(user); // same user
            _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _clientRepo.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync((Client?)null);

            var service = CreateService();

            // Should NOT throw — it's the same user's email
            var result = await service.UpdateAsync(1, new UpdateUserDto { Email = "john@test.com" });
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_EmailTakenByOtherUser_ThrowsArgumentException()
        {
            var currentUser = new User { Id = 1, Username = "john", Email = "john@test.com", PhoneNumber = "0791234567", PasswordHash = "x", Role = AppRoles.Client };
            var otherUser = new User { Id = 2, Username = "jane", Email = "taken@test.com", PhoneNumber = "0799999999", PasswordHash = "x", Role = AppRoles.Client };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(currentUser);
            _userRepo.Setup(r => r.GetByEmailAsync("taken@test.com")).ReturnsAsync(otherUser);

            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UpdateAsync(1, new UpdateUserDto { Email = "taken@test.com" }));
        }
    }
}

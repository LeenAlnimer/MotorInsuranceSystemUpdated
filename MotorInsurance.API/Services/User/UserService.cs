using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.User;
using MotorInsurance.API.Repositories.Client;
using MotorInsurance.API.Repositories.RefreshToken;
using MotorInsurance.API.DTOs.User;
using MotorInsurance.API.Services.Auth;
using MotorInsurance.API.Common;
using Microsoft.Extensions.Logging;

namespace MotorInsurance.API.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IClientRepository _clientRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly JwtService _jwtService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository repository,
            IClientRepository clientRepository,
            IRefreshTokenRepository refreshTokenRepository,
            JwtService jwtService,
            ILogger<UserService> logger)
        {
            _repository = repository;
            _clientRepository = clientRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<Models.User> CreateAsync(CreateUserDto dto)
        {
            await ValidateUniqueFields(dto.Email, dto.PhoneNumber, dto.Username);

            var user = new Models.User
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Client"
            };

            await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            var client = new Models.Client
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                UserId = user.Id
            };

            await _clientRepository.AddAsync(client);
            await _clientRepository.SaveChangesAsync();

            _logger.LogInformation("New client registered: {Username} (UserId={UserId})", user.Username, user.Id);

            return user;
        }

        public async Task<Models.User> CreateEmployeeAsync(CreateEmployeeDto dto)
        {
            await ValidateUniqueFields(dto.Email, dto.PhoneNumber, dto.Username);

            var user = new Models.User
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Employee"
            };

            await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("New employee created: {Username} (UserId={UserId})", user.Username, user.Id);

            return user;
        }

        public async Task<(string Token, string RefreshToken, Models.User User)> LoginAsync(LoginDto dto)
        {
            var user = await _repository.GetByIdentifierAsync(dto.Identifier);

            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for identifier: {Identifier}", dto.Identifier);
                throw new ArgumentException("Invalid credentials");
            }

            user.LastLogin = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            int? clientId = null;
            if (user.Role == "Client")
            {
                var client = await _clientRepository.GetByUserIdAsync(user.Id);
                clientId = client?.Id;
            }

            var token = _jwtService.GenerateToken(user.Id, user.Username, user.Role, clientId);

            var refreshTokenValue = SecurityHelper.GenerateSecureToken();
            var refreshToken = new Models.RefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            _logger.LogInformation("User {Username} logged in (Role={Role})", user.Username, user.Role);

            return (token, refreshTokenValue, user);
        }

        public async Task<List<Models.User>> GetAllAsync() => await _repository.GetAllAsync();

        public async Task<Models.User?> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);

        public async Task<bool> UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user is null) return false;

            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                var existing = await _repository.GetByUsernameAsync(dto.Username);
                if (existing is not null && existing.Id != id)
                    throw new ArgumentException("Username already in use");
                user.Username = dto.Username.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var existing = await _repository.GetByEmailAsync(dto.Email);
                if (existing is not null && existing.Id != id)
                    throw new ArgumentException("Email already in use");
                user.Email = dto.Email.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var existing = await _repository.GetByPhoneAsync(dto.PhoneNumber);
                if (existing is not null && existing.Id != id)
                    throw new ArgumentException("Phone number already in use");
                user.PhoneNumber = dto.PhoneNumber.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            await _repository.SaveChangesAsync();

            if (user.Role == AppRoles.Client)
            {
                var client = await _clientRepository.GetByUserIdAsync(id);
                if (client != null)
                {
                    if (!string.IsNullOrWhiteSpace(dto.Email))
                        client.Email = dto.Email.Trim();
                    if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                        client.PhoneNumber = dto.PhoneNumber.Trim();
                    await _clientRepository.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user is null) return false;

            await _repository.DeleteAsync(user);
            await _repository.SaveChangesAsync();
            return true;
        }

        private async Task ValidateUniqueFields(string email, string phone, string username)
        {
            if (await _repository.GetByEmailAsync(email) is not null)
                throw new ArgumentException("Email already in use");

            if (await _repository.GetByPhoneAsync(phone) is not null)
                throw new ArgumentException("Phone number already in use");

            if (await _repository.GetByUsernameAsync(username) is not null)
                throw new ArgumentException("Username already in use");
        }

    }
}

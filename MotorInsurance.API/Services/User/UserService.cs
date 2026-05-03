using Microsoft.EntityFrameworkCore;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.DTOs.User;
using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.RefreshToken;
using MotorInsurance.API.Repositories.User;
using MotorInsurance.API.Services.Auth;
using Microsoft.Extensions.Logging;

namespace MotorInsurance.API.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly JwtService _jwtService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository repository,
            IRefreshTokenRepository refreshTokenRepository,
            JwtService jwtService,
            ApplicationDbContext context,
            ILogger<UserService> logger)
        {
            _repository = repository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtService = jwtService;
            _context = context;
            _logger = logger;
        }

        public async Task<Models.User> CreateAsync(CreateUserDto dto)
        {
            await ValidateUniqueFields(dto.Email, dto.PhoneNumber, dto.Username);

            var user = new Models.User
            {
                FullName = dto.FullName?.Trim(),
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Client"
            };

            await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("New user registered: {Username} (UserId={UserId})", user.Username, user.Id);

            return user;
        }

        public async Task<Models.User> CreateByAdminAsync(CreateUserByAdminDto dto)
        {
            await ValidateUniqueFields(dto.Email, dto.PhoneNumber, dto.Username);

            var user = new Models.User
            {
                FullName = dto.FullName?.Trim(),
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("User {Username} created with role {Role} by admin", user.Username, dto.Role);

            return user;
        }

        public async Task<(string Token, string RefreshToken, Models.User User)> LoginAsync(LoginDto dto)
        {
            var user = await _repository.GetByIdentifierAsync(dto.EmailOrPhone);

            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for identifier: {Identifier}", dto.EmailOrPhone);
                throw new ArgumentException("Invalid credentials");
            }

            user.LastLogin = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user.Id, user.Username, user.Role);

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

        public async Task<PagedResult<Models.User>> GetAllAsync(UserQueryParams q)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q.Role))
                query = query.Where(u => u.Role == q.Role);

            if (!string.IsNullOrWhiteSpace(q.Search))
                query = query.Where(u =>
                    u.Username.Contains(q.Search) ||
                    u.Email.Contains(q.Search) ||
                    u.PhoneNumber.Contains(q.Search) ||
                    (u.FullName != null && u.FullName.Contains(q.Search)));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.Username)
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .ToListAsync();

            return new PagedResult<Models.User>
            {
                Data = items,
                Page = q.Page,
                PageSize = q.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Models.User?> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);

        public async Task<bool> UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user is null) return false;

            if (dto.FullName != null)
                user.FullName = dto.FullName.Trim();

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
            return true;
        }

        public async Task<bool> UpdateRoleAsync(int id, string role)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user is null) return false;

            user.Role = role;
            await _repository.SaveChangesAsync();

            _logger.LogInformation("User {UserId} role updated to {Role}", id, role);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user is null) return false;

            _repository.Delete(user);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<object> GetStatusAsync()
        {
            var totalPolicies  = await _context.Policies.CountAsync();
            var activePolicies = await _context.Policies.CountAsync(p => p.Status == PolicyStatus.Active);
            var totalClaims    = await _context.Claims.CountAsync();
            var pendingClaims  = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Pending);
            var totalUsers     = await _context.Users.CountAsync();
            var totalQuotes    = await _context.Quotes.CountAsync();
            var approvedQuotes = await _context.Quotes.CountAsync(q => q.Status == QuoteStatus.Approved);

            return new
            {
                policies = new { total = totalPolicies, active = activePolicies },
                claims   = new { total = totalClaims, pending = pendingClaims },
                users    = totalUsers,
                quotes   = new { total = totalQuotes, approved = approvedQuotes }
            };
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

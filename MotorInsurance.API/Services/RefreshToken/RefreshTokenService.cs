using Microsoft.EntityFrameworkCore;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.Repositories.RefreshToken;
using MotorInsurance.API.Repositories.User;
using MotorInsurance.API.Services.Auth;
using System.Data;
using RefreshTokenModel = MotorInsurance.API.Models.RefreshToken;

namespace MotorInsurance.API.Services.RefreshToken
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly JwtService _jwtService;
        private readonly ApplicationDbContext _context;

        public RefreshTokenService(
            IRefreshTokenRepository repository,
            IUserRepository userRepository,
            JwtService jwtService,
            ApplicationDbContext context)
        {
            _repository = repository;
            _userRepository = userRepository;
            _jwtService = jwtService;
            _context = context;
        }

        public async Task<bool> RevokeAsync(string refreshToken)
        {
            var token = await _repository.GetByTokenAsync(refreshToken);
            if (token == null) return false;

            await _repository.DeleteAsync(token);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message, string? NewToken, string? NewRefreshToken)> RefreshAsync(string refreshToken)
        {
            var token = await _repository.GetByTokenAsync(refreshToken);

            if (token == null)
                return (false, "Invalid refresh token", null, null);

            if (token.ExpiryDate < DateTime.UtcNow)
            {
                await _repository.DeleteAsync(token);
                await _repository.SaveChangesAsync();
                return (false, "Refresh token has expired", null, null);
            }

            var user = await _userRepository.GetByIdAsync(token.UserId);
            if (user == null)
                return (false, "User not found", null, null);

            string newJwt;
            string newRefreshTokenValue;

            if (_context.Database.IsRelational())
            {
                // Serializable transaction prevents two concurrent requests from reusing the same token
                using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                await _repository.DeleteAsync(token);

                newJwt = _jwtService.GenerateToken(user.Id, user.Username, user.Role);
                newRefreshTokenValue = SecurityHelper.GenerateSecureToken();
                await _repository.AddAsync(new RefreshTokenModel
                {
                    Token = newRefreshTokenValue,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7)
                });
                await _repository.SaveChangesAsync();
                await tx.CommitAsync();
            }
            else
            {
                await _repository.DeleteAsync(token);
                newJwt = _jwtService.GenerateToken(user.Id, user.Username, user.Role);
                newRefreshTokenValue = SecurityHelper.GenerateSecureToken();
                await _repository.AddAsync(new RefreshTokenModel
                {
                    Token = newRefreshTokenValue,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7)
                });
                await _repository.SaveChangesAsync();
            }

            return (true, "Success", newJwt, newRefreshTokenValue);
        }
    }
}

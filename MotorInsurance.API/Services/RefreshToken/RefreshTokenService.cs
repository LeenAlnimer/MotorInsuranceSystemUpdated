using MotorInsurance.API.Repositories.RefreshToken;
using MotorInsurance.API.Repositories.User;
using MotorInsurance.API.Repositories.Client;
using MotorInsurance.API.Services.Auth;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.RefreshToken;
using RefreshTokenModel = MotorInsurance.API.Models.RefreshToken;

namespace MotorInsurance.API.Services.RefreshToken
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IClientRepository _clientRepository;
        private readonly JwtService _jwtService;

        public RefreshTokenService(
            IRefreshTokenRepository repository,
            IUserRepository userRepository,
            IClientRepository clientRepository,
            JwtService jwtService)
        {
            _repository = repository;
            _userRepository = userRepository;
            _clientRepository = clientRepository;
            _jwtService = jwtService;
        }

        public async Task<List<RefreshTokenResponseDto>> GetAllAsync()
        {
            var tokens = await _repository.GetAllAsync();
            return tokens.Select(t => new RefreshTokenResponseDto
            {
                Id = t.Id,
                UserId = t.UserId,
                ExpiryDate = t.ExpiryDate
            }).ToList();
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

            // Rotate: delete old token
            await _repository.DeleteAsync(token);

            int? clientId = null;
            if (user.Role == "Client")
            {
                var client = await _clientRepository.GetByUserIdAsync(user.Id);
                clientId = client?.Id;
            }

            var newJwt = _jwtService.GenerateToken(user.Id, user.Username, user.Role, clientId);

            var newRefreshTokenValue = SecurityHelper.GenerateSecureToken();
            var newRefreshToken = new RefreshTokenModel
            {
                Token = newRefreshTokenValue,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            await _repository.AddAsync(newRefreshToken);
            await _repository.SaveChangesAsync();

            return (true, "Success", newJwt, newRefreshTokenValue);
        }

    }
}

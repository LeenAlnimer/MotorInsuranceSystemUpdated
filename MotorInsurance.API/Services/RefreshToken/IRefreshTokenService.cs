using MotorInsurance.API.DTOs.RefreshToken;

namespace MotorInsurance.API.Services.RefreshToken
{
    public interface IRefreshTokenService
    {
        Task<List<RefreshTokenResponseDto>> GetAllAsync();
        Task<(bool Success, string Message, string? NewToken, string? NewRefreshToken)> RefreshAsync(string refreshToken);
        Task<bool> RevokeAsync(string refreshToken);
    }
}

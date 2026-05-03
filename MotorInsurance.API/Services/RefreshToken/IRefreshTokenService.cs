namespace MotorInsurance.API.Services.RefreshToken
{
    public interface IRefreshTokenService
    {
        Task<(bool Success, string Message, string? NewToken, string? NewRefreshToken)> RefreshAsync(string refreshToken);
        Task<bool> RevokeAsync(string refreshToken);
    }
}

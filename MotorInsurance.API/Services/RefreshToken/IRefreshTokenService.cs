using RefreshTokenModel = MotorInsurance.API.Models.RefreshToken;

namespace MotorInsurance.API.Services.RefreshToken
{
    public interface IRefreshTokenService
    {
        Task<List<RefreshTokenModel>> GetAllAsync();
        Task<(bool Success, string Message, RefreshTokenModel? Token)> CreateAsync(RefreshTokenModel token);
    }
}

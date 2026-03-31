using RefreshTokenModel = MotorInsurance.API.Models.RefreshToken;
using MotorInsurance.API.Repositories.RefreshToken;

namespace MotorInsurance.API.Services.RefreshToken
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _repository;

        public RefreshTokenService(IRefreshTokenRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RefreshTokenModel>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<(bool Success, string Message, RefreshTokenModel? Token)> CreateAsync(RefreshTokenModel token)
        {
            var userExists = await _repository.UserExists(token.UserId);

            if (!userExists)
                return (false, "User not found", null);

            await _repository.AddAsync(token);
            await _repository.SaveChangesAsync();

            return (true, "Created", token);
        }
    }
}

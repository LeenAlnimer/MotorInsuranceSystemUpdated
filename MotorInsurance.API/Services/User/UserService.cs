using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.User;
using MotorInsurance.API.DTOs.User;

namespace MotorInsurance.API.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<User> CreateAsync(CreateUserDto dto)
        {
            var user = new User
            {
                Username = dto.Username,
                Password = dto.Password
            };

            await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            return user;
        }
    }
}
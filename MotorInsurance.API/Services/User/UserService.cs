using MotorInsurance.API.Models;
using MotorInsurance.API.Repositories.User;
using MotorInsurance.API.DTOs.User;
using MotorInsurance.API.Services.Auth;

namespace MotorInsurance.API.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly JwtService _jwtService;

        public UserService(IUserRepository repository, JwtService jwtService)
        {
            _repository = repository;
            _jwtService = jwtService;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<User> CreateAsync(CreateUserDto dto)
        {
            var users = await _repository.GetAllAsync();

            //  check if username already exists
            var exists = users.Any(u =>
                u.Username.Trim().ToLower() == dto.Username.Trim().ToLower()
            );

            if (exists)
                throw new Exception("Username already exists");

            var user = new User
            {
                Username = dto.Username.Trim(),
                Password = dto.Password.Trim()
            };

            await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            return user;
        }
        public async Task<string> LoginAsync(LoginDto dto)
        {
            var users = await _repository.GetAllAsync();

            var user = users.FirstOrDefault(u =>
                u.Username.Trim().ToLower() == dto.Username.Trim().ToLower() &&
                u.Password.Trim() == dto.Password.Trim()
            );

            if (user == null)
                throw new Exception("Invalid username or password");

            var token = _jwtService.GenerateToken(user.Username);

            return token;
        }

       
        public async Task<User?> GetByIdAsync(int id)
        {
            var users = await _repository.GetAllAsync();
            return users.FirstOrDefault(u => u.Id == id);
        }

        
        public async Task<bool> UpdateAsync(int id, UpdateUserDto dto)
        {
            var users = await _repository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Id == id);

            if (user == null)
                return false;

            user.Username = dto.Username.Trim();
            user.Password = dto.Password.Trim();

            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.Client;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Repositories.Client;
using ClientModel = MotorInsurance.API.Models.Client;

namespace MotorInsurance.API.Services.Client
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientService> _logger;

        public ClientService(IClientRepository repository, ApplicationDbContext context, ILogger<ClientService> logger)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<ClientResponseDto>> GetPagedAsync(ClientQueryParams q)
        {
            var query = _context.Clients.Include(c => c.Cars).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var search = q.Search.ToLower();
                query = query.Where(c =>
                    (c.FullName != null && c.FullName.ToLower().Contains(search)) ||
                    (c.Email != null && c.Email.ToLower().Contains(search)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.FullName)
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .ToListAsync();

            return new PagedResult<ClientResponseDto>
            {
                Data = items.Select(MapToDto).ToList(),
                Page = q.Page,
                PageSize = q.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ClientResponseDto?> GetByIdAsync(int id)
        {
            var c = await _repository.GetByIdAsync(id);
            return c == null ? null : MapToDto(c);
        }

        public async Task<ClientResponseDto> CreateAsync(CreateClientDto dto)
        {
            var client = new ClientModel
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber
            };

            await _repository.AddAsync(client);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Client {ClientId} created: {FullName}", client.Id, client.FullName);

            return MapToDto(client);
        }

        public async Task<bool> UpdateAsync(int id, UpdateClientDto dto)
        {
            var client = await _repository.GetByIdAsync(id);
            if (client == null) return false;

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                client.FullName = dto.FullName;
            if (!string.IsNullOrWhiteSpace(dto.Email))
                client.Email = dto.Email;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                client.PhoneNumber = dto.PhoneNumber;

            _repository.Update(client);
            await _repository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var client = await _repository.GetByIdAsync(id);
            if (client == null) return false;

            _repository.Delete(client);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Client {ClientId} deleted", id);

            return true;
        }

        private static ClientResponseDto MapToDto(ClientModel c) => new ClientResponseDto
        {
            Id = c.Id,
            FullName = c.FullName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            Cars = c.Cars?.Select(car => new CarDto
            {
                Id = car.Id,
                Brand = car.Brand,
                Model = car.Model
            }).ToList()
        };
    }
}

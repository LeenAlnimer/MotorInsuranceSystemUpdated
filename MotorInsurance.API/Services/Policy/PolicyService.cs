using Microsoft.EntityFrameworkCore;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.Policy;

namespace MotorInsurance.API.Services.Policy
{
    public class PolicyService : IPolicyService
    {
        private readonly ApplicationDbContext _context;

        public PolicyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PolicyResponseDto>> GetAllAsync()
        {
            var policies = await _context.Policies
                .Include(p => p.Quote)
                    .ThenInclude(q => q.Car)
                        .ThenInclude(c => c.Client)
                .ToListAsync();

            return policies.Select(p => new PolicyResponseDto
            {
                Id = p.Id,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                QuoteId = p.QuoteId,

                ClientName = p.Quote.Car.Client.FullName,
                CarBrand = p.Quote.Car.Brand,
                CarModel = p.Quote.Car.Model,
                Price = p.Quote.Price
            }).ToList();
        }

        public async Task<PolicyResponseDto?> GetByIdAsync(int id)
        {
            var p = await _context.Policies
                .Include(p => p.Quote)
                    .ThenInclude(q => q.Car)
                        .ThenInclude(c => c.Client)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p == null) return null;

            return new PolicyResponseDto
            {
                Id = p.Id,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                QuoteId = p.QuoteId,

                ClientName = p.Quote.Car.Client.FullName,
                CarBrand = p.Quote.Car.Brand,
                CarModel = p.Quote.Car.Model,
                Price = p.Quote.Price
            };
        }
    }
}
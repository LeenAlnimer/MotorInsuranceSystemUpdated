using MotorInsurance.API.DTOs.Quote;
using MotorInsurance.API.Repositories.Quote;
using MotorInsurance.API.Data;
using Microsoft.EntityFrameworkCore;

namespace MotorInsurance.API.Services.Quote
{
    public class QuoteService : IQuoteService
    {
        private readonly IQuoteRepository _repository;
        private readonly ApplicationDbContext _context;

        public QuoteService(IQuoteRepository repository, ApplicationDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<QuoteResponseDto> CreateAsync(CreateQuoteDto dto)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == dto.CarId);

            if (car == null)
                throw new Exception("Car not found");

            var currentYear = DateTime.Now.Year;
            var carAge = currentYear - car.Year;

            if (carAge > 10)
                throw new Exception("Car not eligible (too old)");

            decimal price = car.Price * 0.05m;

            if (carAge <= 3)
                price *= 1.2m;
            else if (carAge >= 8)
                price *= 0.9m;

            if (car.Price > 30000)
                price *= 1.1m;
            else if (car.Price < 10000)
                price *= 0.95m;

            if (car.FuelType?.ToLower() == "electric")
                price *= 0.9m;
            else if (car.FuelType?.ToLower() == "diesel")
                price *= 1.1m;

            if (price < 300)
                price = 300;

            var quote = new Models.Quote
            {
                CarId = car.Id,
                Price = price,
                CreatedAt = DateTime.Now,
                IsApproved = false
            };

            await _repository.AddAsync(quote);
            await _repository.SaveChangesAsync();

            return new QuoteResponseDto
            {
                Id = quote.Id,
                Price = quote.Price,
                CreatedAt = quote.CreatedAt,
                IsApproved = quote.IsApproved
            };
        }

        // (approve + create policy)
        public async Task<bool> ApproveQuoteAsync(int quoteId)
        {
            var quote = await _context.Quotes
                .Include(q => q.Car)
                .FirstOrDefaultAsync(q => q.Id == quoteId);

            if (quote == null)
                return false;

            if (quote.IsApproved)
                throw new Exception("Quote already approved");

            // approve
            quote.IsApproved = true;

            // create policy
            var policy = new Models.Policy
            {
                QuoteId = quote.Id,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddYears(1)
            };

            await _context.Policies.AddAsync(policy);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
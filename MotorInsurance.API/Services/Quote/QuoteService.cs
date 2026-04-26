using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.DTOs.Quote;
using MotorInsurance.API.Repositories.Policy;
using MotorInsurance.API.Repositories.Quote;

namespace MotorInsurance.API.Services.Quote
{
    public class QuoteService : IQuoteService
    {
        private readonly IQuoteRepository _repository;
        private readonly IPolicyRepository _policyRepository;
        private readonly ApplicationDbContext _context;
        private readonly InsurancePricingSettings _pricing;
        private readonly ILogger<QuoteService> _logger;

        public QuoteService(
            IQuoteRepository repository,
            IPolicyRepository policyRepository,
            ApplicationDbContext context,
            IOptions<InsurancePricingSettings> pricing,
            ILogger<QuoteService> logger)
        {
            _repository = repository;
            _policyRepository = policyRepository;
            _context = context;
            _pricing = pricing.Value;
            _logger = logger;
        }

        public async Task<QuoteResponseDto> CreateAsync(CreateQuoteDto dto, int? clientId = null)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == dto.CarId);
            if (car == null)
                throw new ArgumentException("Car not found");

            if (clientId.HasValue && car.ClientId != clientId.Value)
                throw new UnauthorizedAccessException("You do not own this car");

            var carAge = DateTime.UtcNow.Year - car.Year;
            if (carAge > _pricing.MaxCarAgeYears)
                throw new ArgumentException("Car is not eligible for insurance (too old)");

            decimal price = car.Price * _pricing.BaseRatePercent;

            if (carAge <= _pricing.NewCarMaxAge)          price *= _pricing.NewCarMultiplier;
            else if (carAge >= _pricing.OldCarMinAge)     price *= _pricing.OldCarMultiplier;

            if (car.Price > _pricing.HighPriceThreshold)       price *= _pricing.HighPriceMultiplier;
            else if (car.Price < _pricing.LowPriceThreshold)   price *= _pricing.LowPriceMultiplier;

            if (car.FuelType == FuelType.Electric)       price *= _pricing.ElectricMultiplier;
            else if (car.FuelType == FuelType.Diesel)    price *= _pricing.DieselMultiplier;

            if (price < _pricing.MinimumPremium) price = _pricing.MinimumPremium;

            var quote = new Models.Quote
            {
                CarId = car.Id,
                Price = price,
                CreatedAt = DateTime.UtcNow,
                IsApproved = false
            };

            await _repository.AddAsync(quote);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Quote {QuoteId} created for Car {CarId} | Price: {Price:C}", quote.Id, car.Id, price);

            return MapToDto(quote);
        }

        public async Task<PagedResult<QuoteResponseDto>> GetPagedAsync(QuoteQueryParams q)
        {
            return await BuildPagedResult(_repository.GetQueryable(), q);
        }

        public async Task<PagedResult<QuoteResponseDto>> GetPagedByClientIdAsync(int clientId, QuoteQueryParams q)
        {
            var query = _repository.GetQueryable()
                .Include(quote => quote.Car)
                .Where(quote => quote.Car != null && quote.Car.ClientId == clientId);

            return await BuildPagedResult(query, q);
        }

        public async Task<QuoteResponseDto?> GetByIdAsync(int id)
        {
            var q = await _repository.GetByIdAsync(id);
            return q == null ? null : MapToDto(q);
        }

        public async Task<bool> ApproveQuoteAsync(int quoteId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var quote = await _repository.GetByIdWithCarAsync(quoteId);

            if (quote == null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            if (quote.IsApproved)
                throw new ArgumentException("Quote is already approved");

            quote.IsApproved = true;

            var policy = new Models.Policy
            {
                QuoteId = quote.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1)
            };

            await _policyRepository.AddAsync(policy);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Quote {QuoteId} approved → Policy {PolicyId} created", quoteId, policy.Id);

            return true;
        }

        public async Task<bool> RejectQuoteAsync(int id)
        {
            var quote = await _repository.GetByIdAsync(id);
            if (quote == null) return false;

            if (quote.IsApproved)
                throw new ArgumentException("Cannot reject an already approved quote");

            _repository.Remove(quote);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Quote {QuoteId} rejected", id);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var quote = await _repository.GetByIdAsync(id);
            if (quote == null) return false;

            _repository.Remove(quote);
            await _repository.SaveChangesAsync();

            return true;
        }

        private async Task<PagedResult<QuoteResponseDto>> BuildPagedResult(
            IQueryable<Models.Quote> query, QuoteQueryParams q)
        {
            if (q.IsApproved.HasValue)
                query = query.Where(x => x.IsApproved == q.IsApproved.Value);

            query = q.SortBy?.ToLower() switch
            {
                "price"     => q.SortOrder == "desc" ? query.OrderByDescending(x => x.Price)     : query.OrderBy(x => x.Price),
                "createdat" => q.SortOrder == "desc" ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                _           => query.OrderByDescending(x => x.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .ToListAsync();

            return new PagedResult<QuoteResponseDto>
            {
                Data = items.Select(MapToDto).ToList(),
                Page = q.Page,
                PageSize = q.PageSize,
                TotalCount = totalCount
            };
        }

        private static QuoteResponseDto MapToDto(Models.Quote q) => new QuoteResponseDto
        {
            Id = q.Id,
            CarId = q.CarId,
            Price = q.Price,
            CreatedAt = q.CreatedAt,
            IsApproved = q.IsApproved
        };
    }
}

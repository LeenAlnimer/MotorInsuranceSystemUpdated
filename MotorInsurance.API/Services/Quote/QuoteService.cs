using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.DTOs.Quote;
using MotorInsurance.API.Repositories.Quote;

namespace MotorInsurance.API.Services.Quote
{
    public class QuoteService : IQuoteService
    {
        private readonly IQuoteRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly InsurancePricingSettings _pricing;
        private readonly ILogger<QuoteService> _logger;

        public QuoteService(
            IQuoteRepository repository,
            ApplicationDbContext context,
            IOptions<InsurancePricingSettings> pricing,
            ILogger<QuoteService> logger)
        {
            _repository = repository;
            _context = context;
            _pricing = pricing.Value;
            _logger = logger;
        }

        public async Task<QuoteResponseDto> CreateAsync(CreateQuoteDto dto, int? userId = null)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == dto.CarId);
            if (car == null)
                throw new ArgumentException("Car not found");

            if (userId.HasValue && car.UserId != userId.Value)
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

            var now = DateTime.UtcNow;
            var quote = new Models.Quote
            {
                CarId = car.Id,
                Price = price,
                CreatedAt = now,
                ExpiresAt = now.AddDays(30),
                Status = QuoteStatus.Pending
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

        public async Task<PagedResult<QuoteResponseDto>> GetPagedByUserIdAsync(int userId, QuoteQueryParams q)
        {
            var query = _repository.GetQueryable()
                .Include(quote => quote.Car)
                .Where(quote => quote.Car != null && quote.Car.UserId == userId);

            return await BuildPagedResult(query, q);
        }

        public async Task<QuoteResponseDto?> GetByIdAsync(int id, int? restrictToUserId = null)
        {
            var q = await _repository.GetByIdWithCarAsync(id);
            if (q == null) return null;

            if (restrictToUserId.HasValue && q.Car?.UserId != restrictToUserId.Value)
                throw new UnauthorizedAccessException("Quote does not belong to you");

            return MapToDto(q);
        }

        public async Task<bool> ApproveQuoteAsync(int quoteId)
        {
            var quote = await _repository.GetByIdWithCarAsync(quoteId);
            if (quote == null) return false;

            if (quote.Status == QuoteStatus.Approved)
                throw new ArgumentException("Quote is already approved");

            if (quote.Status == QuoteStatus.Rejected)
                throw new ArgumentException("Cannot approve a rejected quote");

            if (quote.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Quote has expired and cannot be approved");

            if (_context.Database.IsRelational())
            {
                using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                await ApproveInternalAsync(quote, quoteId);
                await tx.CommitAsync();
            }
            else
            {
                await ApproveInternalAsync(quote, quoteId);
            }

            return true;
        }

        private async Task ApproveInternalAsync(Models.Quote quote, int quoteId)
        {
            var carHasActivePolicy = await _context.Policies
                .AnyAsync(p => p.Quote != null &&
                               p.Quote.CarId == quote.CarId &&
                               p.Status == PolicyStatus.Active &&
                               p.EndDate > DateTime.UtcNow);

            if (carHasActivePolicy)
                throw new InvalidOperationException("This car already has an active policy");

            quote.Status = QuoteStatus.Approved;

            var otherPendingQuotes = await _context.Quotes
                .Where(q => q.CarId == quote.CarId &&
                            q.Id != quoteId &&
                            q.Status == QuoteStatus.Pending)
                .ToListAsync();

            foreach (var other in otherPendingQuotes)
                other.Status = QuoteStatus.Rejected;

            var policy = new Models.Policy
            {
                QuoteId = quote.Id,
                InsuredValue = quote.Car!.Price,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1),
                Status = PolicyStatus.Active
            };

            _context.Policies.Add(policy);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Quote {QuoteId} approved → Policy {PolicyId} created | {CancelledCount} other quotes cancelled",
                quoteId, policy.Id, otherPendingQuotes.Count);
        }

        public async Task<bool> RejectQuoteAsync(int id)
        {
            var quote = await _repository.GetByIdAsync(id);
            if (quote == null) return false;

            if (quote.Status == QuoteStatus.Approved)
                throw new ArgumentException("Cannot reject an already approved quote");

            if (quote.Status == QuoteStatus.Rejected)
                throw new ArgumentException("Quote is already rejected");

            quote.Status = QuoteStatus.Rejected;
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Quote {QuoteId} rejected", id);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var quote = await _repository.GetByIdAsync(id);
            if (quote == null) return false;

            // نمنع الحذف فقط لو في بوليصة نشطة - الملغية والمنتهية لا تمنع الحذف
            var hasActivePolicy = await _context.Policies
                .AnyAsync(p => p.QuoteId == id && p.Status == PolicyStatus.Active);
            if (hasActivePolicy)
                throw new InvalidOperationException("Cannot delete a quote that has an active policy.");

            _repository.Remove(quote);
            await _repository.SaveChangesAsync();

            return true;
        }

        private async Task<PagedResult<QuoteResponseDto>> BuildPagedResult(
            IQueryable<Models.Quote> query, QuoteQueryParams q)
        {
            if (q.Status.HasValue)
                query = query.Where(x => x.Status == q.Status.Value);

            if (q.ExcludeExpired)
                query = query.Where(x => x.Status != QuoteStatus.Pending || x.ExpiresAt >= DateTime.UtcNow);

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
            ExpiresAt = q.ExpiresAt,
            Status = q.Status
        };
    }
}

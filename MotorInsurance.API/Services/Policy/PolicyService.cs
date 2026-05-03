using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.Policy;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Repositories.Policy;
using PolicyModel = MotorInsurance.API.Models.Policy;
using QuoteModel = MotorInsurance.API.Models.Quote;

namespace MotorInsurance.API.Services.Policy
{
    public class PolicyService : IPolicyService
    {
        private readonly IPolicyRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly InsurancePricingSettings _pricing;
        private readonly ILogger<PolicyService> _logger;

        public PolicyService(
            IPolicyRepository repository,
            ApplicationDbContext context,
            IOptions<InsurancePricingSettings> pricing,
            ILogger<PolicyService> logger)
        {
            _repository = repository;
            _context = context;
            _pricing = pricing.Value;
            _logger = logger;
        }

        public async Task<PagedResult<PolicyResponseDto>> GetPagedAsync(PolicyQueryParams q)
        {
            var query = _repository.GetQueryable();
            return await BuildPagedResult(query, q);
        }

        public async Task<PagedResult<PolicyResponseDto>> GetPagedByUserIdAsync(int userId, PolicyQueryParams q)
        {
            var query = _repository.GetQueryable()
                .Where(p => p.Quote != null && p.Quote.Car != null && p.Quote.Car.UserId == userId);
            return await BuildPagedResult(query, q);
        }

        public async Task<PolicyResponseDto?> GetByIdAsync(int id)
        {
            var p = await _repository.GetByIdAsync(id);

            if (p == null)
            {
                _logger.LogWarning("Policy {PolicyId} not found", id);
                return null;
            }

            return MapToDto(p);
        }

        private async Task<PagedResult<PolicyResponseDto>> BuildPagedResult(
            IQueryable<Models.Policy> query, PolicyQueryParams q)
        {
            if (q.Status.HasValue)
                query = query.Where(p => p.Status == q.Status.Value);

            query = q.SortBy?.ToLower() switch
            {
                "enddate"   => q.SortOrder == "desc" ? query.OrderByDescending(p => p.EndDate)   : query.OrderBy(p => p.EndDate),
                "startdate" => q.SortOrder == "desc" ? query.OrderByDescending(p => p.StartDate) : query.OrderBy(p => p.StartDate),
                _           => query.OrderByDescending(p => p.StartDate)
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .ToListAsync();

            return new PagedResult<PolicyResponseDto>
            {
                Data = items.Select(MapToDto).ToList(),
                Page = q.Page,
                PageSize = q.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PolicyResponseDto> RenewAsync(int policyId)
        {
            var existing = await _repository.GetByIdAsync(policyId);
            if (existing == null)
                throw new KeyNotFoundException($"Policy {policyId} not found");

            if (existing.Status == PolicyStatus.Cancelled)
                throw new InvalidOperationException("Cancelled policies cannot be renewed");

            if (existing.Status == PolicyStatus.Active && existing.EndDate > DateTime.UtcNow)
                throw new InvalidOperationException("Policy is still active and has not expired yet");

            var car = existing.Quote?.Car
                ?? throw new InvalidOperationException("Policy has no associated car data");

            var carAge = DateTime.UtcNow.Year - car.Year;
            if (carAge > _pricing.MaxCarAgeYears)
                throw new InvalidOperationException("Car is no longer eligible for insurance renewal (too old)");

            decimal price = car.Price * _pricing.BaseRatePercent;

            if (carAge <= _pricing.NewCarMaxAge)        price *= _pricing.NewCarMultiplier;
            else if (carAge >= _pricing.OldCarMinAge)   price *= _pricing.OldCarMultiplier;

            if (car.Price > _pricing.HighPriceThreshold)      price *= _pricing.HighPriceMultiplier;
            else if (car.Price < _pricing.LowPriceThreshold)  price *= _pricing.LowPriceMultiplier;

            if (car.FuelType == FuelType.Electric)      price *= _pricing.ElectricMultiplier;
            else if (car.FuelType == FuelType.Diesel)   price *= _pricing.DieselMultiplier;

            if (price < _pricing.MinimumPremium) price = _pricing.MinimumPremium;

            var now = DateTime.UtcNow;
            var renewalQuote = new QuoteModel
            {
                CarId = car.Id,
                Price = price,
                CreatedAt = now,
                ExpiresAt = now.AddYears(1),
                Status = QuoteStatus.Approved
            };

            _context.Quotes.Add(renewalQuote);

            var renewed = new PolicyModel
            {
                Quote = renewalQuote,
                InsuredValue = car.Price,
                StartDate = now,
                EndDate = now.AddYears(1),
                Status = PolicyStatus.Active
            };

            await _repository.AddAsync(renewed);
            await _repository.SaveChangesAsync();

            _logger.LogInformation(
                "Policy {OldPolicyId} renewed as Policy {NewPolicyId} | New Quote price: {Price:C}",
                policyId, renewed.Id, price);

            renewed.Quote = renewalQuote;
            renewalQuote.Car = car;
            return MapToDto(renewed);
        }

        public async Task<PolicyResponseDto> CancelAsync(int policyId, int performedByUserId)
        {
            var policy = await _repository.GetByIdAsync(policyId);
            if (policy == null)
                throw new KeyNotFoundException($"Policy {policyId} not found");

            if (policy.Status != PolicyStatus.Active)
                throw new InvalidOperationException("Only active policies can be cancelled");

            if (policy.EndDate < DateTime.UtcNow)
                throw new InvalidOperationException("Policy has already expired and cannot be cancelled");

            policy.Status = PolicyStatus.Cancelled;

            // رفض كل المطالبات المعلقة تلقائياً عند إلغاء البوليصة
            var pendingClaims = await _context.Claims
                .Where(c => c.PolicyId == policyId && c.Status == ClaimStatus.Pending)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var claim in pendingClaims)
            {
                claim.Status = ClaimStatus.Rejected;
                claim.RejectedById = performedByUserId;
                claim.RejectedAt = now;
            }

            await _repository.SaveChangesAsync();

            _logger.LogInformation(
                "Policy {PolicyId} cancelled by User {UserId} | {Count} pending claims auto-rejected",
                policyId, performedByUserId, pendingClaims.Count);

            return MapToDto(policy);
        }

        private static PolicyResponseDto MapToDto(Models.Policy p) => new()
        {
            Id = p.Id,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status == PolicyStatus.Active && p.EndDate < DateTime.UtcNow
                ? PolicyStatus.Expired
                : p.Status,
            QuoteId = p.QuoteId,
            UserId = p.Quote?.Car?.UserId,
            UserName = p.Quote?.Car?.User?.Username,
            CarBrand = p.Quote?.Car?.Brand,
            CarModel = p.Quote?.Car?.Model,
            Price = p.Quote?.Price ?? 0,
            InsuredValue = p.InsuredValue
        };
    }
}

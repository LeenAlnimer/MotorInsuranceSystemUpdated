using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.Policy;
using MotorInsurance.API.DTOs.QueryParams;

namespace MotorInsurance.API.Services.Policy
{
    public class PolicyService : IPolicyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PolicyService> _logger;

        public PolicyService(ApplicationDbContext context, ILogger<PolicyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<PolicyResponseDto>> GetPagedAsync(PolicyQueryParams q)
        {
            var query = _context.Policies
                .Include(p => p.Quote)
                    .ThenInclude(q => q!.Car)
                        .ThenInclude(c => c!.Client)
                .AsQueryable();

            return await BuildPagedResult(query, q);
        }

        public async Task<PagedResult<PolicyResponseDto>> GetPagedByClientIdAsync(int clientId, PolicyQueryParams q)
        {
            var query = _context.Policies
                .Include(p => p.Quote)
                    .ThenInclude(q => q!.Car)
                        .ThenInclude(c => c!.Client)
                .Where(p => p.Quote != null && p.Quote.Car != null && p.Quote.Car.ClientId == clientId);

            return await BuildPagedResult(query, q);
        }

        public async Task<PolicyResponseDto?> GetByIdAsync(int id)
        {
            var p = await _context.Policies
                .Include(p => p.Quote)
                    .ThenInclude(q => q!.Car)
                        .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(p => p.Id == id);

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

        public async Task<(bool Success, string Message)> CancelAsync(int id)
        {
            var policy = await _context.Policies.FindAsync(id);
            if (policy == null)
                return (false, "Policy not found");

            if (policy.EndDate < DateTime.UtcNow)
                return (false, "Policy is already expired");

            policy.EndDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Policy {PolicyId} cancelled", id);

            return (true, "Policy cancelled");
        }

        private static PolicyResponseDto MapToDto(Models.Policy p) => new PolicyResponseDto
        {
            Id = p.Id,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            QuoteId = p.QuoteId,
            ClientName = p.Quote?.Car?.Client?.FullName,
            CarBrand = p.Quote?.Car?.Brand,
            CarModel = p.Quote?.Car?.Model,
            Price = p.Quote?.Price ?? 0
        };
    }
}

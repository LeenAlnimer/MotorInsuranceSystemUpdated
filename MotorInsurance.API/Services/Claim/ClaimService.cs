using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.Claim;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Repositories.Claim;
using MotorInsurance.API.Services.Email;
using ClaimModel = MotorInsurance.API.Models.Claim;


namespace MotorInsurance.API.Services.Claim
{
    public class ClaimService : IClaimService
    {
        private readonly IClaimRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ClaimService> _logger;

        public ClaimService(IClaimRepository repository, ApplicationDbContext context, IEmailService emailService, ILogger<ClaimService> logger)
        {
            _repository = repository;
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<PagedResult<ClaimResponseDto>> GetPagedAsync(ClaimQueryParams q)
        {
            return await BuildPagedResult(_repository.GetQueryable(), q);
        }

        public async Task<PagedResult<ClaimResponseDto>> GetPagedByUserIdAsync(int userId, ClaimQueryParams q)
        {
            var query = _repository.GetQueryable().Where(c => c.UserId == userId);
            return await BuildPagedResult(query, q);
        }

        public async Task<ClaimResponseDto?> GetByIdAsync(int id)
        {
            var claim = await _repository.GetByIdAsync(id);
            return claim == null ? null : MapToDto(claim);
        }

        public async Task<(bool Success, string Message, ClaimResponseDto? Claim)> CreateAsync(CreateClaimDto dto, int userId)
        {
            var policy = await _context.Policies.FindAsync(dto.PolicyId);
            if (policy == null)
                return (false, "Policy not found", null);

            if (policy.EndDate < DateTime.UtcNow)
                return (false, "Policy has expired", null);

            if (!await _repository.UserExists(userId))
                return (false, "User not found", null);

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client != null)
            {
                var policyBelongsToClient = await _context.Policies
                    .AnyAsync(p => p.Id == dto.PolicyId &&
                                   p.Quote != null &&
                                   p.Quote.Car != null &&
                                   p.Quote.Car.ClientId == client.Id);

                if (!policyBelongsToClient)
                    return (false, "Policy does not belong to you", null);
            }

            var claim = new ClaimModel
            {
                Description = dto.Description,
                Status = ClaimStatus.Pending,
                PolicyId = dto.PolicyId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(claim);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Claim {ClaimId} created for Policy {PolicyId} by User {UserId}",
                claim.Id, dto.PolicyId, userId);

            return (true, "Created", MapToDto(claim));
        }

        public async Task<bool> ApproveAsync(int id)
        {
            var claim = await _repository.GetByIdAsync(id);
            if (claim == null) return false;

            claim.Status = ClaimStatus.Approved;
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Claim {ClaimId} approved", id);

            var user = await _context.Users.FindAsync(claim.UserId);
            if (user != null)
                _ = _emailService.SendAsync(
                    user.Email,
                    "تم الموافقة على مطالبتك — Motor Insurance",
                    $"عزيزي {user.Username}،\n\nتم الموافقة على مطالبتك رقم #{claim.Id}.\n\nشكراً لثقتك بنا.");

            return true;
        }

        public async Task<bool> RejectAsync(int id)
        {
            var claim = await _repository.GetByIdAsync(id);
            if (claim == null) return false;

            claim.Status = ClaimStatus.Rejected;
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Claim {ClaimId} rejected", id);

            var user = await _context.Users.FindAsync(claim.UserId);
            if (user != null)
                _ = _emailService.SendAsync(
                    user.Email,
                    "تم رفض مطالبتك — Motor Insurance",
                    $"عزيزي {user.Username}،\n\nنأسف لإعلامك بأنه تم رفض مطالبتك رقم #{claim.Id}.\n\nللاستفسار يرجى التواصل معنا.");

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var claim = await _repository.GetByIdAsync(id);
            if (claim == null) return false;

            _repository.Delete(claim);
            await _repository.SaveChangesAsync();
            return true;
        }

        private async Task<PagedResult<ClaimResponseDto>> BuildPagedResult(
            IQueryable<ClaimModel> query, ClaimQueryParams q)
        {
            if (q.Status.HasValue)
                query = query.Where(c => c.Status == q.Status.Value);

            if (q.UserId.HasValue)
                query = query.Where(c => c.UserId == q.UserId.Value);

            if (q.FromDate.HasValue)
                query = query.Where(c => c.CreatedAt >= q.FromDate.Value);

            if (q.ToDate.HasValue)
                query = query.Where(c => c.CreatedAt <= q.ToDate.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .ToListAsync();

            return new PagedResult<ClaimResponseDto>
            {
                Data = items.Select(MapToDto).ToList(),
                Page = q.Page,
                PageSize = q.PageSize,
                TotalCount = totalCount
            };
        }

        private static ClaimResponseDto MapToDto(ClaimModel c) => new ClaimResponseDto
        {
            Id = c.Id,
            Description = c.Description,
            Status = c.Status.ToString(),
            PolicyId = c.PolicyId,
            UserId = c.UserId,
            CreatedAt = c.CreatedAt
        };
    }
}

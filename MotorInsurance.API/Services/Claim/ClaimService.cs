using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;
using MotorInsurance.API.DTOs.Claim;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Repositories.Claim;
using MotorInsurance.API.Services.Email;
using System.Data;
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
            var policy = await _context.Policies
                .Include(p => p.Quote)
                    .ThenInclude(q => q!.Car)
                .FirstOrDefaultAsync(p => p.Id == dto.PolicyId);

            if (policy == null)
                return (false, "Policy not found", null);

            if (policy.Status != PolicyStatus.Active)
                return (false, $"Policy is {policy.Status.ToString().ToLower()} and cannot accept new claims", null);

            if (policy.EndDate < DateTime.UtcNow)
                return (false, "Policy has expired", null);

            if (!await _repository.UserExists(userId))
                return (false, "User not found", null);

            var car = policy.Quote?.Car;
            if (car == null)
                return (false, "Policy has no associated car data", null);

            if (car.UserId != userId)
                return (false, "Policy does not belong to you", null);

            // استخدام InsuredValue المحفوظ وقت إنشاء البوليصة - fallback لسعر السيارة الحالي للبوليصات القديمة
            var insuredValue = policy.InsuredValue > 0 ? policy.InsuredValue : car.Price;

            if (dto.ClaimAmount > insuredValue)
                return (false, $"Claim amount cannot exceed the insured value ({insuredValue:F2})", null);

            ClaimModel claim;

            if (_context.Database.IsRelational())
            {
                using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var totalActiveClaims = await _context.Claims
                    .Where(c => c.PolicyId == dto.PolicyId &&
                                (c.Status == ClaimStatus.Pending || c.Status == ClaimStatus.Approved))
                    .SumAsync(c => (decimal?)c.ClaimAmount) ?? 0;

                if (totalActiveClaims + dto.ClaimAmount > insuredValue)
                    return (false,
                        $"Total claims ({totalActiveClaims + dto.ClaimAmount:F2}) would exceed the insured value ({insuredValue:F2})",
                        null);

                claim = new ClaimModel
                {
                    Description = dto.Description,
                    ClaimAmount = dto.ClaimAmount,
                    Status = ClaimStatus.Pending,
                    PolicyId = dto.PolicyId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.AddAsync(claim);
                await _repository.SaveChangesAsync();
                await tx.CommitAsync();
            }
            else
            {
                var totalActiveClaims = await _context.Claims
                    .Where(c => c.PolicyId == dto.PolicyId &&
                                (c.Status == ClaimStatus.Pending || c.Status == ClaimStatus.Approved))
                    .SumAsync(c => (decimal?)c.ClaimAmount) ?? 0;

                if (totalActiveClaims + dto.ClaimAmount > insuredValue)
                    return (false,
                        $"Total claims ({totalActiveClaims + dto.ClaimAmount:F2}) would exceed the insured value ({insuredValue:F2})",
                        null);

                claim = new ClaimModel
                {
                    Description = dto.Description,
                    ClaimAmount = dto.ClaimAmount,
                    Status = ClaimStatus.Pending,
                    PolicyId = dto.PolicyId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.AddAsync(claim);
                await _repository.SaveChangesAsync();
            }

            _logger.LogInformation("Claim {ClaimId} created for Policy {PolicyId} by User {UserId}",
                claim.Id, dto.PolicyId, userId);

            return (true, "Created", MapToDto(claim));
        }

        public async Task<bool> ApproveAsync(int id, int performedByUserId)
        {
            var claim = await _repository.GetByIdAsync(id);
            if (claim == null) return false;

            if (claim.Status != ClaimStatus.Pending)
                throw new InvalidOperationException($"Claim is already {claim.Status.ToString().ToLower()}");

            var policy = await _context.Policies.FindAsync(claim.PolicyId);
            if (policy == null || policy.Status != PolicyStatus.Active || policy.EndDate < DateTime.UtcNow)
                throw new InvalidOperationException("Cannot approve a claim for a policy that is not active");

            claim.Status = ClaimStatus.Approved;
            claim.ApprovedById = performedByUserId;
            claim.ApprovedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Claim {ClaimId} approved by User {UserId}", id, performedByUserId);

            var user = await _context.Users.FindAsync(claim.UserId);
            if (user != null)
            {
                try
                {
                    await _emailService.SendAsync(
                        user.Email,
                        "تم الموافقة على مطالبتك — Motor Insurance",
                        $"عزيزي {user.Username}،\n\nتم الموافقة على مطالبتك رقم #{claim.Id}.\n\nشكراً لثقتك بنا.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send approval email for Claim {ClaimId}", id);
                }
            }

            return true;
        }

        public async Task<bool> RejectAsync(int id, int performedByUserId)
        {
            var claim = await _repository.GetByIdAsync(id);
            if (claim == null) return false;

            if (claim.Status != ClaimStatus.Pending)
                throw new InvalidOperationException($"Claim is already {claim.Status.ToString().ToLower()}");

            claim.Status = ClaimStatus.Rejected;
            claim.RejectedById = performedByUserId;
            claim.RejectedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Claim {ClaimId} rejected by User {UserId}", id, performedByUserId);

            var user = await _context.Users.FindAsync(claim.UserId);
            if (user != null)
            {
                try
                {
                    await _emailService.SendAsync(
                        user.Email,
                        "تم رفض مطالبتك — Motor Insurance",
                        $"عزيزي {user.Username}،\n\nنأسف لإعلامك بأنه تم رفض مطالبتك رقم #{claim.Id}.\n\nللاستفسار يرجى التواصل معنا.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send rejection email for Claim {ClaimId}", id);
                }
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var claim = await _repository.GetByIdAsync(id);
            if (claim == null) return false;

            if (claim.Status == ClaimStatus.Pending || claim.Status == ClaimStatus.Approved)
                throw new InvalidOperationException(
                    $"Cannot delete a {claim.Status.ToString().ToLower()} claim. Only rejected claims can be deleted.");

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

            if (q.PolicyId.HasValue)
                query = query.Where(c => c.PolicyId == q.PolicyId.Value);

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
            ClaimAmount = c.ClaimAmount,
            Status = c.Status,
            PolicyId = c.PolicyId,
            UserId = c.UserId,
            CreatedAt = c.CreatedAt,
            ApprovedById = c.ApprovedById,
            ApprovedAt = c.ApprovedAt,
            RejectedById = c.RejectedById,
            RejectedAt = c.RejectedAt
        };
    }
}

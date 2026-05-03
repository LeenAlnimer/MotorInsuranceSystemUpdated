using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Quote;
using MotorInsurance.API.DTOs.QueryParams;

namespace MotorInsurance.API.Services.Quote
{
    public interface IQuoteService
    {
        Task<PagedResult<QuoteResponseDto>> GetPagedAsync(QuoteQueryParams queryParams);
        Task<PagedResult<QuoteResponseDto>> GetPagedByUserIdAsync(int userId, QuoteQueryParams queryParams);
        Task<QuoteResponseDto?> GetByIdAsync(int id, int? restrictToUserId = null);
        Task<QuoteResponseDto> CreateAsync(CreateQuoteDto dto, int? userId = null);
        Task<bool> ApproveQuoteAsync(int quoteId);
        Task<bool> RejectQuoteAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}

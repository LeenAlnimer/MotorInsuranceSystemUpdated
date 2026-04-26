using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Quote;
using MotorInsurance.API.DTOs.QueryParams;

namespace MotorInsurance.API.Services.Quote
{
    public interface IQuoteService
    {
        Task<PagedResult<QuoteResponseDto>> GetPagedAsync(QuoteQueryParams queryParams);
        Task<PagedResult<QuoteResponseDto>> GetPagedByClientIdAsync(int clientId, QuoteQueryParams queryParams);
        Task<QuoteResponseDto?> GetByIdAsync(int id);
        Task<QuoteResponseDto> CreateAsync(CreateQuoteDto dto, int? clientId = null);
        Task<bool> ApproveQuoteAsync(int quoteId);
        Task<bool> RejectQuoteAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}

using MotorInsurance.API.DTOs.Quote;

namespace MotorInsurance.API.Services.Quote
{
    public interface IQuoteService
    {
        Task<QuoteResponseDto> CreateAsync(CreateQuoteDto dto);

        Task<bool> ApproveQuoteAsync(int quoteId);

        // NEW
        Task<List<QuoteResponseDto>> GetAllAsync();
        Task<QuoteResponseDto?> GetByIdAsync(int id);
        Task<bool> RejectQuoteAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}
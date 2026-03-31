using MotorInsurance.API.DTOs.Quote;

namespace MotorInsurance.API.Services.Quote
{
    public interface IQuoteService
    {
        Task<QuoteResponseDto> CreateAsync(CreateQuoteDto dto);

        Task<bool> ApproveQuoteAsync(int quoteId);
    }
}
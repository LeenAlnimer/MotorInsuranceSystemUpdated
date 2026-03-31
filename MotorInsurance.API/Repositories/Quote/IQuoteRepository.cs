using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Quote
{
    public interface IQuoteRepository
    {
        Task<Models.Quote?> GetByIdAsync(int id);
        Task AddAsync(Models.Quote quote);
        Task SaveChangesAsync();
    }
}
using backend.Models;

namespace backend.Services;

public interface IQuoteService
{
    Task<Quote?> GetRandomQuoteAsync();
    Task<Quote?> GetRandomQuoteByCategoryAsync(string category);
    Task<List<Quote>> GetAllQuotesAsync();
    Task<Quote?> GetQuoteByIdAsync(int id);
    Task<Quote> AddQuoteAsync(Quote quote);
    Task<Quote?> UpdateQuoteAsync(int id, Quote quote);
    Task<bool> DeleteQuoteAsync(int id);
    Task<int> SeedQuotesAsync();
}
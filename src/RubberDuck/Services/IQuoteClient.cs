using RubberDuck.Models;

namespace RubberDuck.Services;

public interface IQuoteClient
{
    Task<Quote> GetRandomQuoteAsync(CancellationToken ct = default);
}
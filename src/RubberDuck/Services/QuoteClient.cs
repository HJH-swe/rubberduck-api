using System.Net.Http.Json;
using RubberDuck.Models;

namespace RubberDuck.Services;

public class QuoteClient(HttpClient http, IConfiguration config) : IQuoteClient
{
    public async Task<Quote> GetRandomQuoteAsync(CancellationToken ct = default)
    {
        var apiKey = config["ApiNinjas:Key"] 
            ?? throw new InvalidOperationException("ApiNinjas:Key is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Get, "randomquotes?categories=courage");
        request.Headers.Add("X-Api-Key", apiKey);

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var quotes = await response.Content.ReadFromJsonAsync<List<ApiQuote>>(ct);
        var first = quotes?.FirstOrDefault() 
            ?? throw new InvalidOperationException("API Ninjas returned no quotes.");
        
        return new Quote(first.Quote, first.Author);
    }            

    private record ApiQuote(string Quote, string Author);
}
using System.Net.Http.Json;

namespace RubberDuck.Services;

public class DuckImageClient (HttpClient http) : IDuckImageClient {
    
    public async Task<string> GetRandomDuckUrlAsync(CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<RandomDuckResponse>("random", ct)
            ?? throw new InvalidOperationException("random-d.uk returned an empty response.");

        return response.Url;
    }

    private record RandomDuckResponse(string Url, string Message);
}
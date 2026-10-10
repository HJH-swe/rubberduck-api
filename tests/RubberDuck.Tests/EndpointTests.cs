using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RubberDuck.Models;
using RubberDuck.Services;

namespace RubberDuck.Tests;

public class EndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDuckImageClient>();
            services.RemoveAll<IQuoteClient>();
            services.AddSingleton<IDuckImageClient, FakeDuckImageClient>();
            services.AddSingleton<IQuoteClient, FakeQuoteClient>();
        });
    }).CreateClient();

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Duck_ReturnsDuckAndQuote()
    {
        var advice = await _client.GetFromJsonAsync<DuckAdvice>("/api/duck");

        Assert.NotNull(advice);
        Assert.Equal("https://example.test/duck.jpg", advice.DuckImageUrl);
        Assert.Equal("Be brave.", advice.Wisdom.Text);
    }

    private class FakeDuckImageClient : IDuckImageClient
    {
        public Task<string> GetRandomDuckUrlAsync(CancellationToken ct = default)
            => Task.FromResult("https://example.test/duck.jpg");
    }

    private class FakeQuoteClient : IQuoteClient
    {
        public Task<Quote> GetRandomQuoteAsync(CancellationToken ct = default)
            => Task.FromResult(new Quote("Be brave.", "Test Author"));
    }

}
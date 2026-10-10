using Microsoft.Extensions.Configuration;
using RubberDuck.Models;
using RubberDuck.Services;

namespace RubberDuck.Tests;

public class QuoteClientTest
{
    [Fact]
    public async Task GetRandomQuoteAsync_WithoutApiKey_ThrowsError()
    {
        // Arrange - empty config without "ApiNinjas:Key"
        var config = new ConfigurationBuilder().Build();
        var client = new QuoteClient(new HttpClient(), config);

        // Act
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetRandomQuoteAsync());

        // Assert
        Assert.Contains("ApiNinjas:Key", error.Message);
    }
}
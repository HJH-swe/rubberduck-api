using Azure.Identity;
using RubberDuck.Models;
using RubberDuck.Services;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

builder.Services.AddHttpClient<IDuckImageClient, DuckImageClient>(client =>
{
    client.BaseAddress = new Uri("https://random-d.uk/api/v2/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<IQuoteClient, QuoteClient>(client =>
{
    client.BaseAddress = new Uri("https://api.api-ninjas.com/v2/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/duck", async (IDuckImageClient ducks, IQuoteClient quotes, CancellationToken ct) => {
   var url = await ducks.GetRandomDuckUrlAsync(ct);
   var wisdom = await quotes.GetRandomQuoteAsync(ct);
   return Results.Ok(new DuckAdvice(url, wisdom));
});

app.Run();
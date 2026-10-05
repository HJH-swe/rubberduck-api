using RubberDuck.Models;
using RubberDuck.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IDuckImageClient, DuckImageClient>(client =>
{
    client.BaseAddress = new Uri("https://random-d.uk/api/v2/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/duck", async (IDuckImageClient ducks, CancellationToken ct) => {
   var url = await ducks.GetRandomDuckUrlAsync(ct);
   return Results.Ok(new DuckAdvice(url));
});

app.Run();
namespace RubberDuck.Services;

public interface IDuckImageClient
{
    Task<string> GetRandomDuckUrlAsync(CancellationToken ct = default);
    
}
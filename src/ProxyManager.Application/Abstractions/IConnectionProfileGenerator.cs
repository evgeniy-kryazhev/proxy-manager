namespace ProxyManager.Application.Abstractions;

public interface IConnectionProfileGenerator
{
    string GenerateUri(string username, string password, string? label);
}

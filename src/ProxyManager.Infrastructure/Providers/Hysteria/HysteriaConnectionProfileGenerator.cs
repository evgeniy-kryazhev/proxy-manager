using ProxyManager.Application.Abstractions;
using ProxyManager.Infrastructure.Configuration;

namespace ProxyManager.Infrastructure.Providers.Hysteria;

public sealed class HysteriaConnectionProfileGenerator : IConnectionProfileGenerator
{
    private readonly HysteriaOptions _options;

    public HysteriaConnectionProfileGenerator(HysteriaOptions options)
    {
        _options = options;
    }

    public string GenerateUri(string username, string password, string? label)
    {
        var encodedLabel = Uri.EscapeDataString(string.IsNullOrWhiteSpace(label) ? username : label);
        var encodedUsername = Uri.EscapeDataString(username);
        var encodedPassword = Uri.EscapeDataString(password);

        return $"hysteria2://{encodedUsername}:{encodedPassword}@{_options.Host}:{_options.Port}?sni={Uri.EscapeDataString(_options.Sni)}&insecure=0#{encodedLabel}";
    }
}

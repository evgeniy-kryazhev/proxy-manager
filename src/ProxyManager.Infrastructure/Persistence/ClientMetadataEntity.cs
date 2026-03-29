using ProxyManager.Domain.Clients;

namespace ProxyManager.Infrastructure.Persistence;

public sealed class ClientMetadataEntity
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ClientStatus Status { get; set; }

    public string Provider { get; set; } = "Hysteria2";

    public DateTimeOffset? LastPasswordRegeneratedAt { get; set; }
}

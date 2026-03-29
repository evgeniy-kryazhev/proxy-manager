using ProxyManager.Domain.Clients;

namespace ProxyManager.Application.Abstractions;

public sealed record ClientMetadataRecord(
    string Username,
    string? DisplayName,
    DateTimeOffset CreatedAt,
    ClientStatus Status,
    string Provider,
    DateTimeOffset? LastPasswordRegeneratedAt);

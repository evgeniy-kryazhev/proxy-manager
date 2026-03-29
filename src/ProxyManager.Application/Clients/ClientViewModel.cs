using ProxyManager.Domain.Clients;

namespace ProxyManager.Application.Clients;

public sealed record ClientViewModel(
    string Username,
    string? DisplayName,
    DateTimeOffset CreatedAt,
    ClientStatus Status,
    string Provider,
    string Password,
    string Uri,
    bool ExistsInRuntime);

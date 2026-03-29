namespace ProxyManager.Application.Abstractions;

public interface IProxyRuntimeConfigGateway
{
    Task<IReadOnlyCollection<RuntimeClientRecord>> GetClientsAsync(CancellationToken cancellationToken);

    Task AddClientAsync(string username, string password, CancellationToken cancellationToken);

    Task RemoveClientAsync(string username, CancellationToken cancellationToken);

    Task UpdateClientPasswordAsync(string username, string newPassword, CancellationToken cancellationToken);
}

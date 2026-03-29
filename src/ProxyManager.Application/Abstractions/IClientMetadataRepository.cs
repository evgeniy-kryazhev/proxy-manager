namespace ProxyManager.Application.Abstractions;

public interface IClientMetadataRepository
{
    Task<IReadOnlyCollection<ClientMetadataRecord>> GetAllAsync(CancellationToken cancellationToken);

    Task<ClientMetadataRecord?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    Task UpsertAsync(ClientMetadataRecord metadata, CancellationToken cancellationToken);
}

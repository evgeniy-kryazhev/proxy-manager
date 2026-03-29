using Microsoft.EntityFrameworkCore;
using ProxyManager.Application.Abstractions;

namespace ProxyManager.Infrastructure.Persistence;

public sealed class EfClientMetadataRepository : IClientMetadataRepository
{
    private readonly ProxyManagerDbContext _dbContext;

    public EfClientMetadataRepository(ProxyManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ClientMetadataRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ClientMetadata
            .AsNoTracking()
            .Select(x => new ClientMetadataRecord(
                x.Username,
                x.DisplayName,
                x.CreatedAt,
                x.Status,
                x.Provider,
                x.LastPasswordRegeneratedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ClientMetadataRecord?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return await _dbContext.ClientMetadata
            .AsNoTracking()
            .Where(x => x.Username == username)
            .Select(x => new ClientMetadataRecord(
                x.Username,
                x.DisplayName,
                x.CreatedAt,
                x.Status,
                x.Provider,
                x.LastPasswordRegeneratedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(ClientMetadataRecord metadata, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ClientMetadata
            .SingleOrDefaultAsync(x => x.Username == metadata.Username, cancellationToken);

        if (entity is null)
        {
            entity = new ClientMetadataEntity
            {
                Username = metadata.Username,
                DisplayName = metadata.DisplayName,
                CreatedAt = metadata.CreatedAt,
                Status = metadata.Status,
                Provider = metadata.Provider,
                LastPasswordRegeneratedAt = metadata.LastPasswordRegeneratedAt
            };
            _dbContext.ClientMetadata.Add(entity);
        }
        else
        {
            entity.DisplayName = metadata.DisplayName;
            entity.CreatedAt = metadata.CreatedAt;
            entity.Status = metadata.Status;
            entity.Provider = metadata.Provider;
            entity.LastPasswordRegeneratedAt = metadata.LastPasswordRegeneratedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

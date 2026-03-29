using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProxyManager.Infrastructure.Identity;

namespace ProxyManager.Infrastructure.Persistence;

public sealed class ProxyManagerDbContext : IdentityDbContext<ApplicationUser>
{
    public ProxyManagerDbContext(DbContextOptions<ProxyManagerDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClientMetadataEntity> ClientMetadata => Set<ClientMetadataEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ClientMetadataEntity>(entity =>
        {
            entity.ToTable("ClientMetadata");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.Username).HasMaxLength(64).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(128);
            entity.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        });
    }
}

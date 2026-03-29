using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProxyManager.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProxyManagerDbContext>
{
    public ProxyManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProxyManagerDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ProxyManager;Trusted_Connection=True;TrustServerCertificate=True");
        return new ProxyManagerDbContext(optionsBuilder.Options);
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyManager.Application.Abstractions;
using ProxyManager.Infrastructure.Configuration;
using ProxyManager.Infrastructure.Identity;
using ProxyManager.Infrastructure.Persistence;
using ProxyManager.Infrastructure.Providers.Hysteria;
using ProxyManager.Infrastructure.Runtime;

namespace ProxyManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=ProxyManager;Trusted_Connection=True;TrustServerCertificate=True";

        services.AddDbContext<ProxyManagerDbContext>(options =>
        {
            if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
                && connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
                return;
            }

            options.UseSqlServer(connectionString);
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
            })
            .AddEntityFrameworkStores<ProxyManagerDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.AddAuthorization();

        var hysteriaOptions = new HysteriaOptions();
        configuration.GetSection(HysteriaOptions.SectionName).Bind(hysteriaOptions);
        services.AddSingleton(hysteriaOptions);

        services.AddScoped<IClientMetadataRepository, EfClientMetadataRepository>();
        services.AddScoped<IProxyRuntimeConfigGateway, HysteriaRuntimeConfigGateway>();
        services.AddSingleton<IPasswordGenerator, CryptographicPasswordGenerator>();
        services.AddSingleton<IConnectionProfileGenerator, HysteriaConnectionProfileGenerator>();

        services.AddScoped<MediatR.INotificationHandler<Application.Clients.ProxyConfigurationChangedNotification>, HysteriaDockerComposeReaction>();

        services.AddHostedService<AdminUserSeeder>();

        return services;
    }
}

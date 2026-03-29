using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyManager.Infrastructure.Identity;

public sealed class AdminUserSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public AdminUserSeeder(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var username = _configuration["AdminUser:Username"];
        var password = _configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var existing = await userManager.FindByNameAsync(username);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = username,
            Email = $"{username}@local"
        };

        await userManager.CreateAsync(user, password);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

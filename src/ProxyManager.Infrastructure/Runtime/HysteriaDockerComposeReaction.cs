using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using ProxyManager.Application.Clients;
using ProxyManager.Infrastructure.Configuration;

namespace ProxyManager.Infrastructure.Runtime;

public sealed class HysteriaDockerComposeReaction : INotificationHandler<ProxyConfigurationChangedNotification>
{
    private readonly HysteriaOptions _options;
    private readonly ILogger<HysteriaDockerComposeReaction> _logger;

    public HysteriaDockerComposeReaction(HysteriaOptions options, ILogger<HysteriaDockerComposeReaction> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task Handle(ProxyConfigurationChangedNotification notification, CancellationToken cancellationToken)
    {
        if (!string.Equals(notification.Provider, "Hysteria2", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            WorkingDirectory = _options.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("compose");
        startInfo.ArgumentList.Add("restart");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start docker compose process.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError("docker compose restart failed. Output: {Output}; Error: {Error}", output, error);
            throw new InvalidOperationException($"docker compose restart failed with code {process.ExitCode}: {error}");
        }

        _logger.LogInformation("docker compose restart completed. Output: {Output}", output);
    }
}

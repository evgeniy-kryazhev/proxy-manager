using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using ProxyManager.Application.Clients;
using ProxyManager.Infrastructure.Abstractions;
using ProxyManager.Infrastructure.Configuration;
using ProxyManager.Infrastructure.Providers.Hysteria;

namespace ProxyManager.Infrastructure.Runtime;

public sealed class HysteriaDockerComposeReaction : INotificationHandler<ProxyConfigurationChangedNotification>
{
    private readonly IEnumerable<IProxyRuntimeApplier> _runtimeAppliers;
    private readonly ILogger<HysteriaDockerComposeReaction> _logger;

    public HysteriaDockerComposeReaction(
        IEnumerable<IProxyRuntimeApplier> runtimeAppliers,
        ILogger<HysteriaDockerComposeReaction> logger)
    {
        _runtimeAppliers = runtimeAppliers;
        _logger = logger;
    }

    public async Task Handle(ProxyConfigurationChangedNotification notification, CancellationToken cancellationToken)
    {
        var applier = _runtimeAppliers.FirstOrDefault(x =>
            string.Equals(x.Provider, notification.Provider, StringComparison.OrdinalIgnoreCase));
        if (applier is null)
        {
            _logger.LogWarning("No runtime applier registered for provider {Provider}", notification.Provider);
            return;
        }

        await applier.ApplyAsync(cancellationToken);
    }
}

public sealed class HysteriaDockerComposeRuntimeApplier : IProxyRuntimeApplier
{
    private readonly HysteriaOptions _options;
    private readonly ILogger<HysteriaDockerComposeRuntimeApplier> _logger;

    public HysteriaDockerComposeRuntimeApplier(HysteriaOptions options, ILogger<HysteriaDockerComposeRuntimeApplier> logger)
    {
        _options = options;
        _logger = logger;
    }

    public string Provider => HysteriaProvider.Name;

    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
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
            _logger.LogError("docker compose restart failed for provider {Provider}. Output: {Output}; Error: {Error}", Provider, output, error);
            throw new InvalidOperationException($"docker compose restart failed with code {process.ExitCode}: {error}");
        }

        _logger.LogInformation("docker compose restart completed for provider {Provider}. Output: {Output}", Provider, output);
    }
}

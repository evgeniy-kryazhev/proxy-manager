namespace ProxyManager.Infrastructure.Abstractions;

public interface IProxyRuntimeApplier
{
    string Provider { get; }

    Task ApplyAsync(CancellationToken cancellationToken);
}

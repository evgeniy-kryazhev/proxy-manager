using ProxyManager.Application.Abstractions;
using ProxyManager.Infrastructure.Configuration;
using YamlDotNet.RepresentationModel;

namespace ProxyManager.Infrastructure.Providers.Hysteria;

public sealed class HysteriaRuntimeConfigGateway : IProxyRuntimeConfigGateway
{
    private readonly HysteriaOptions _options;

    public HysteriaRuntimeConfigGateway(HysteriaOptions options)
    {
        _options = options;
    }

    public async Task<IReadOnlyCollection<RuntimeClientRecord>> GetClientsAsync(CancellationToken cancellationToken)
    {
        var (stream, userpass) = await LoadStreamAndUserpassAsync(cancellationToken);
        return userpass
            .OfType<YamlMappingNode>()
            .Select(item => new RuntimeClientRecord(
                Username: ReadValue(item, "username"),
                Password: ReadValue(item, "password"),
                Provider: "Hysteria2"))
            .Where(x => !string.IsNullOrWhiteSpace(x.Username))
            .ToArray();
    }

    public async Task AddClientAsync(string username, string password, CancellationToken cancellationToken)
    {
        var (stream, userpass) = await LoadStreamAndUserpassAsync(cancellationToken);

        if (userpass.OfType<YamlMappingNode>().Any(x => string.Equals(ReadValue(x, "username"), username, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Client '{username}' already exists in runtime config.");
        }

        userpass.Add(new YamlMappingNode
        {
            { "username", username },
            { "password", password }
        });

        await SaveAsync(stream, cancellationToken);
    }

    public async Task RemoveClientAsync(string username, CancellationToken cancellationToken)
    {
        var (stream, userpass) = await LoadStreamAndUserpassAsync(cancellationToken);

        var toRemove = userpass
            .OfType<YamlMappingNode>()
            .Where(x => string.Equals(ReadValue(x, "username"), username, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var item in toRemove)
        {
            userpass.Children.Remove(item);
        }

        if (toRemove.Length > 0)
        {
            await SaveAsync(stream, cancellationToken);
        }
    }

    public async Task UpdateClientPasswordAsync(string username, string newPassword, CancellationToken cancellationToken)
    {
        var (stream, userpass) = await LoadStreamAndUserpassAsync(cancellationToken);
        var existing = userpass
            .OfType<YamlMappingNode>()
            .FirstOrDefault(x => string.Equals(ReadValue(x, "username"), username, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            throw new InvalidOperationException($"Client '{username}' not found in runtime config.");
        }

        existing.Children[new YamlScalarNode("password")] = new YamlScalarNode(newPassword);
        await SaveAsync(stream, cancellationToken);
    }

    private async Task<(YamlStream Stream, YamlSequenceNode Userpass)> LoadStreamAndUserpassAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_options.ConfigPath))
        {
            throw new FileNotFoundException($"Hysteria config file not found: {_options.ConfigPath}");
        }

        var yaml = await File.ReadAllTextAsync(_options.ConfigPath, cancellationToken);
        var stream = new YamlStream();
        using var input = new StringReader(yaml);
        stream.Load(input);

        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode root)
        {
            throw new InvalidOperationException("Invalid Hysteria YAML config format.");
        }

        var auth = GetOrCreateMapping(root, "auth");
        var type = GetOrCreateScalar(auth, "type", "userpass");
        if (!string.Equals(type.Value, "userpass", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only auth.type=userpass is supported.");
        }

        var userpass = GetOrCreateSequence(auth, "userpass");
        return (stream, userpass);
    }

    private static YamlMappingNode GetOrCreateMapping(YamlMappingNode parent, string key)
    {
        var keyNode = new YamlScalarNode(key);
        if (parent.Children.TryGetValue(keyNode, out var node) && node is YamlMappingNode mapping)
        {
            return mapping;
        }

        var created = new YamlMappingNode();
        parent.Children[keyNode] = created;
        return created;
    }

    private static YamlSequenceNode GetOrCreateSequence(YamlMappingNode parent, string key)
    {
        var keyNode = new YamlScalarNode(key);
        if (parent.Children.TryGetValue(keyNode, out var node) && node is YamlSequenceNode sequence)
        {
            return sequence;
        }

        var created = new YamlSequenceNode();
        parent.Children[keyNode] = created;
        return created;
    }

    private static YamlScalarNode GetOrCreateScalar(YamlMappingNode parent, string key, string defaultValue)
    {
        var keyNode = new YamlScalarNode(key);
        if (parent.Children.TryGetValue(keyNode, out var node) && node is YamlScalarNode scalar)
        {
            return scalar;
        }

        var created = new YamlScalarNode(defaultValue);
        parent.Children[keyNode] = created;
        return created;
    }

    private static string ReadValue(YamlMappingNode node, string key)
    {
        return node.Children.TryGetValue(new YamlScalarNode(key), out var value) && value is YamlScalarNode scalar
            ? scalar.Value ?? string.Empty
            : string.Empty;
    }

    private async Task SaveAsync(YamlStream stream, CancellationToken cancellationToken)
    {
        await using var output = File.CreateText(_options.ConfigPath);
        stream.Save(output, assignAnchors: false);
        await output.FlushAsync(cancellationToken);
    }
}

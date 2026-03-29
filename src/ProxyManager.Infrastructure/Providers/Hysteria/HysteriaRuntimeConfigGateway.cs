using ProxyManager.Application.Abstractions;
using ProxyManager.Infrastructure.Configuration;
using YamlDotNet.RepresentationModel;

namespace ProxyManager.Infrastructure.Providers.Hysteria;

public sealed class HysteriaRuntimeConfigGateway : IProxyRuntimeConfigGateway
{
    private static readonly YamlScalarNode UserpassKey = new("userpass");
    private static readonly YamlScalarNode UsernameKey = new("username");
    private static readonly YamlScalarNode PasswordKey = new("password");

    private readonly HysteriaOptions _options;

    public HysteriaRuntimeConfigGateway(HysteriaOptions options)
    {
        _options = options;
    }

    public async Task<IReadOnlyCollection<RuntimeClientRecord>> GetClientsAsync(CancellationToken cancellationToken)
    {
        var (stream, auth) = await LoadStreamAndAuthAsync(cancellationToken);
        var users = EnumerateUserpassEntries(auth);

        return users
            .Select(item => new RuntimeClientRecord(
                Username: item.Username,
                Password: item.Password,
                Provider: HysteriaProvider.Name))
            .Where(x => !string.IsNullOrWhiteSpace(x.Username))
            .ToArray();
    }

    public async Task AddClientAsync(string username, string password, CancellationToken cancellationToken)
    {
        var (stream, auth) = await LoadStreamAndAuthAsync(cancellationToken);
        var userpass = GetOrCreateUserpassMap(auth);

        if (ContainsUser(userpass, username))
        {
            throw new InvalidOperationException($"Client '{username}' already exists in runtime config.");
        }

        userpass.Children[new YamlScalarNode(username)] = new YamlScalarNode(password);

        await SaveAsync(stream, cancellationToken);
    }

    public async Task RemoveClientAsync(string username, CancellationToken cancellationToken)
    {
        var (stream, auth) = await LoadStreamAndAuthAsync(cancellationToken);
        var userpass = GetOrCreateUserpassMap(auth);
        var keyToRemove = GetExistingUsernameKey(userpass, username);

        if (keyToRemove is not null)
        {
            userpass.Children.Remove(keyToRemove);
            await SaveAsync(stream, cancellationToken);
        }
    }

    public async Task UpdateClientPasswordAsync(string username, string newPassword, CancellationToken cancellationToken)
    {
        var (stream, auth) = await LoadStreamAndAuthAsync(cancellationToken);
        var userpass = GetOrCreateUserpassMap(auth);
        var existingKey = GetExistingUsernameKey(userpass, username);
        if (existingKey is null)
        {
            throw new InvalidOperationException($"Client '{username}' not found in runtime config.");
        }

        userpass.Children[existingKey] = new YamlScalarNode(newPassword);
        await SaveAsync(stream, cancellationToken);
    }

    private async Task<(YamlStream Stream, YamlMappingNode Auth)> LoadStreamAndAuthAsync(CancellationToken cancellationToken)
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

        return (stream, auth);
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

    private static YamlMappingNode GetOrCreateUserpassMap(YamlMappingNode auth)
    {
        if (auth.Children.TryGetValue(UserpassKey, out var existingNode))
        {
            if (existingNode is YamlMappingNode mapping)
            {
                return mapping;
            }

            if (existingNode is YamlSequenceNode sequence)
            {
                var converted = ConvertSequenceToMap(sequence);
                auth.Children[UserpassKey] = converted;
                return converted;
            }
        }

        var created = new YamlMappingNode();
        auth.Children[UserpassKey] = created;
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

    private static IReadOnlyCollection<(string Username, string Password)> EnumerateUserpassEntries(YamlMappingNode auth)
    {
        if (!auth.Children.TryGetValue(UserpassKey, out var node))
        {
            return Array.Empty<(string Username, string Password)>();
        }

        if (node is YamlMappingNode map)
        {
            return map.Children
                .OfType<KeyValuePair<YamlNode, YamlNode>>()
                .Select(x => (
                    Username: (x.Key as YamlScalarNode)?.Value ?? string.Empty,
                    Password: (x.Value as YamlScalarNode)?.Value ?? string.Empty))
                .Where(x => !string.IsNullOrWhiteSpace(x.Username))
                .ToArray();
        }

        if (node is YamlSequenceNode sequence)
        {
            return sequence
                .OfType<YamlMappingNode>()
                .Select(item => (
                    Username: ReadValue(item, UsernameKey),
                    Password: ReadValue(item, PasswordKey)))
                .Where(x => !string.IsNullOrWhiteSpace(x.Username))
                .ToArray();
        }

        return Array.Empty<(string Username, string Password)>();
    }

    private static YamlMappingNode ConvertSequenceToMap(YamlSequenceNode sequence)
    {
        var converted = new YamlMappingNode();
        foreach (var item in sequence.OfType<YamlMappingNode>())
        {
            var username = ReadValue(item, UsernameKey);
            if (string.IsNullOrWhiteSpace(username))
            {
                continue;
            }

            converted.Children[new YamlScalarNode(username)] = new YamlScalarNode(ReadValue(item, PasswordKey));
        }

        return converted;
    }

    private static bool ContainsUser(YamlMappingNode userpass, string username)
    {
        return GetExistingUsernameKey(userpass, username) is not null;
    }

    private static YamlScalarNode? GetExistingUsernameKey(YamlMappingNode userpass, string username)
    {
        return userpass.Children.Keys
            .OfType<YamlScalarNode>()
            .FirstOrDefault(x => string.Equals(x.Value, username, StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadValue(YamlMappingNode node, YamlScalarNode key)
    {
        return node.Children.TryGetValue(key, out var value) && value is YamlScalarNode scalar
            ? scalar.Value ?? string.Empty
            : string.Empty;
    }

    private async Task SaveAsync(YamlStream stream, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_options.ConfigPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Invalid Hysteria config path directory.");
        }

        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $"{Path.GetFileName(_options.ConfigPath)}.{Guid.NewGuid():N}.tmp");
        await using (var output = File.CreateText(tempPath))
        {
            stream.Save(output, assignAnchors: false);
            await output.FlushAsync(cancellationToken);
        }

        if (File.Exists(_options.ConfigPath))
        {
            var backupPath = $"{_options.ConfigPath}.bak";
            File.Copy(_options.ConfigPath, backupPath, overwrite: true);
            File.Move(tempPath, _options.ConfigPath, overwrite: true);
            return;
        }

        File.Move(tempPath, _options.ConfigPath);
    }
}

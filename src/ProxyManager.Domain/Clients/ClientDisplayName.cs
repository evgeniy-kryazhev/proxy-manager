namespace ProxyManager.Domain.Clients;

public sealed record ClientDisplayName
{
    public string? Value { get; }

    private ClientDisplayName(string? value)
    {
        Value = value;
    }

    public static ClientDisplayName Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ClientDisplayName((string?)null);
        }

        var normalized = value.Trim();
        if (normalized.Length > 128)
        {
            throw new ArgumentException("Display name is too long.", nameof(value));
        }

        return new ClientDisplayName(normalized);
    }

    public override string ToString() => Value ?? string.Empty;
}

namespace ProxyManager.Domain.Clients;

public sealed record ClientUsername
{
    public string Value { get; }

    private ClientUsername(string value)
    {
        Value = value;
    }

    public static ClientUsername Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Username is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length > 64)
        {
            throw new ArgumentException("Username is too long.", nameof(value));
        }

        return new ClientUsername(normalized);
    }

    public override string ToString() => Value;
}

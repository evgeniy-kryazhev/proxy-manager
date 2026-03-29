namespace ProxyManager.Domain.Clients;

public sealed record ClientPassword
{
    public string Value { get; }

    private ClientPassword(string value)
    {
        Value = value;
    }

    public static ClientPassword Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Password is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length < 12)
        {
            throw new ArgumentException("Password must be at least 12 characters.", nameof(value));
        }

        return new ClientPassword(normalized);
    }

    public override string ToString() => Value;
}

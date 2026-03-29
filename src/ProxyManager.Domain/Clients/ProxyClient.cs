namespace ProxyManager.Domain.Clients;

public sealed class ProxyClient
{
    public ProxyClient(
        ClientUsername username,
        ClientPassword password,
        ClientDisplayName displayName,
        DateTimeOffset createdAt,
        ClientStatus status,
        string provider,
        bool fromRuntimeConfig)
    {
        Username = username;
        Password = password;
        DisplayName = displayName;
        CreatedAt = createdAt;
        Status = status;
        Provider = provider;
        FromRuntimeConfig = fromRuntimeConfig;
    }

    public ClientUsername Username { get; }

    public ClientPassword Password { get; private set; }

    public ClientDisplayName DisplayName { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public ClientStatus Status { get; private set; }

    public string Provider { get; }

    public bool FromRuntimeConfig { get; }

    public void RegeneratePassword(ClientPassword newPassword) => Password = newPassword;

    public void SetDisplayName(ClientDisplayName displayName) => DisplayName = displayName;

    public void MarkDeleted() => Status = ClientStatus.Deleted;
}

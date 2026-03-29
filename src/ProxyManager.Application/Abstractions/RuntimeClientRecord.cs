namespace ProxyManager.Application.Abstractions;

public sealed record RuntimeClientRecord(
    string Username,
    string Password,
    string Provider);

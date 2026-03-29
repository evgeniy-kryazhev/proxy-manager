using MediatR;

namespace ProxyManager.Application.Clients;

public sealed record ProxyConfigurationChangedNotification(string Provider) : INotification;

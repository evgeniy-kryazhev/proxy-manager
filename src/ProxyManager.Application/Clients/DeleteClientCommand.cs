using MediatR;

namespace ProxyManager.Application.Clients;

public sealed record DeleteClientCommand(string Username) : IRequest;

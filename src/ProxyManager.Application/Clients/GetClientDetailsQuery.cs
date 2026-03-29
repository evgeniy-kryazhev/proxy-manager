using MediatR;

namespace ProxyManager.Application.Clients;

public sealed record GetClientDetailsQuery(string Username) : IRequest<ClientViewModel?>;

using MediatR;

namespace ProxyManager.Application.Clients;

public sealed record GetClientsQuery : IRequest<IReadOnlyCollection<ClientViewModel>>;

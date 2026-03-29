using MediatR;

namespace ProxyManager.Application.Clients;

public sealed record RegenerateClientPasswordCommand(string Username) : IRequest<ClientViewModel>;

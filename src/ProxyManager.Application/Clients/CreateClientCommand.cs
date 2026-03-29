using MediatR;

namespace ProxyManager.Application.Clients;

public sealed record CreateClientCommand(string Username, string? DisplayName) : IRequest<ClientViewModel>;

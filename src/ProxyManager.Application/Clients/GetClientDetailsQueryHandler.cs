using MediatR;

namespace ProxyManager.Application.Clients;

public sealed class GetClientDetailsQueryHandler : IRequestHandler<GetClientDetailsQuery, ClientViewModel?>
{
    private readonly IMediator _mediator;

    public GetClientDetailsQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ClientViewModel?> Handle(GetClientDetailsQuery request, CancellationToken cancellationToken)
    {
        var clients = await _mediator.Send(new GetClientsQuery(), cancellationToken);
        return clients.FirstOrDefault(x => string.Equals(x.Username, request.Username, StringComparison.OrdinalIgnoreCase));
    }
}

using MediatR;
using ProxyManager.Application.Abstractions;
using ProxyManager.Domain.Clients;

namespace ProxyManager.Application.Clients;

public sealed class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand>
{
    private readonly IProxyRuntimeConfigGateway _runtimeConfigGateway;
    private readonly IClientMetadataRepository _metadataRepository;
    private readonly IMediator _mediator;

    public DeleteClientCommandHandler(
        IProxyRuntimeConfigGateway runtimeConfigGateway,
        IClientMetadataRepository metadataRepository,
        IMediator mediator)
    {
        _runtimeConfigGateway = runtimeConfigGateway;
        _metadataRepository = metadataRepository;
        _mediator = mediator;
    }

    public async Task Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        var username = ClientUsername.Create(request.Username).Value;
        await _runtimeConfigGateway.RemoveClientAsync(username, cancellationToken);

        var metadata = await _metadataRepository.GetByUsernameAsync(username, cancellationToken)
            ?? new ClientMetadataRecord(username, null, DateTimeOffset.UtcNow, ClientStatus.Active, "Hysteria2", null);

        await _metadataRepository.UpsertAsync(metadata with { Status = ClientStatus.Deleted }, cancellationToken);
        await _mediator.Publish(new ProxyConfigurationChangedNotification("Hysteria2"), cancellationToken);
    }
}

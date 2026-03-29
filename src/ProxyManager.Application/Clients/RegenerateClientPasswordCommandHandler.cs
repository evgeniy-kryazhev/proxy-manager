using MediatR;
using ProxyManager.Application.Abstractions;
using ProxyManager.Domain.Clients;

namespace ProxyManager.Application.Clients;

public sealed class RegenerateClientPasswordCommandHandler : IRequestHandler<RegenerateClientPasswordCommand, ClientViewModel>
{
    private readonly IProxyRuntimeConfigGateway _runtimeConfigGateway;
    private readonly IClientMetadataRepository _metadataRepository;
    private readonly IPasswordGenerator _passwordGenerator;
    private readonly IConnectionProfileGenerator _connectionProfileGenerator;
    private readonly IMediator _mediator;

    public RegenerateClientPasswordCommandHandler(
        IProxyRuntimeConfigGateway runtimeConfigGateway,
        IClientMetadataRepository metadataRepository,
        IPasswordGenerator passwordGenerator,
        IConnectionProfileGenerator connectionProfileGenerator,
        IMediator mediator)
    {
        _runtimeConfigGateway = runtimeConfigGateway;
        _metadataRepository = metadataRepository;
        _passwordGenerator = passwordGenerator;
        _connectionProfileGenerator = connectionProfileGenerator;
        _mediator = mediator;
    }

    public async Task<ClientViewModel> Handle(RegenerateClientPasswordCommand request, CancellationToken cancellationToken)
    {
        var username = ClientUsername.Create(request.Username).Value;
        var password = _passwordGenerator.GenerateStrongPassword();
        ClientPassword.Create(password);

        await _runtimeConfigGateway.UpdateClientPasswordAsync(username, password, cancellationToken);

        var metadata = await _metadataRepository.GetByUsernameAsync(username, cancellationToken)
            ?? new ClientMetadataRecord(username, null, DateTimeOffset.UtcNow, ClientStatus.Active, "Hysteria2", null);

        metadata = metadata with
        {
            Status = ClientStatus.Active,
            LastPasswordRegeneratedAt = DateTimeOffset.UtcNow
        };

        await _metadataRepository.UpsertAsync(metadata, cancellationToken);
        await _mediator.Publish(new ProxyConfigurationChangedNotification("Hysteria2"), cancellationToken);

        return new ClientViewModel(
            Username: username,
            DisplayName: metadata.DisplayName,
            CreatedAt: metadata.CreatedAt,
            Status: metadata.Status,
            Provider: metadata.Provider,
            Password: password,
            Uri: _connectionProfileGenerator.GenerateUri(username, password, metadata.DisplayName),
            ExistsInRuntime: true);
    }
}

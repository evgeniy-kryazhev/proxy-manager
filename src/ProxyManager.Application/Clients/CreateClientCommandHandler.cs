using MediatR;
using ProxyManager.Application.Abstractions;
using ProxyManager.Domain.Clients;

namespace ProxyManager.Application.Clients;

public sealed class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, ClientViewModel>
{
    private readonly IProxyRuntimeConfigGateway _runtimeConfigGateway;
    private readonly IClientMetadataRepository _metadataRepository;
    private readonly IPasswordGenerator _passwordGenerator;
    private readonly IConnectionProfileGenerator _connectionProfileGenerator;
    private readonly IMediator _mediator;

    public CreateClientCommandHandler(
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

    public async Task<ClientViewModel> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var username = ClientUsername.Create(request.Username).Value;
        var displayName = ClientDisplayName.Create(request.DisplayName).Value;

        var existing = await _metadataRepository.GetByUsernameAsync(username, cancellationToken);
        if (existing is not null && existing.Status == ClientStatus.Active)
        {
            throw new InvalidOperationException($"Client '{username}' already exists.");
        }

        var password = _passwordGenerator.GenerateStrongPassword();
        password = ClientPassword.Create(password).Value;

        await _runtimeConfigGateway.AddClientAsync(username, password, cancellationToken);

        var metadata = new ClientMetadataRecord(
            Username: username,
            DisplayName: displayName,
            CreatedAt: existing?.CreatedAt ?? DateTimeOffset.UtcNow,
            Status: ClientStatus.Active,
            Provider: "Hysteria2",
            LastPasswordRegeneratedAt: null);

        await _metadataRepository.UpsertAsync(metadata, cancellationToken);
        await _mediator.Publish(new ProxyConfigurationChangedNotification("Hysteria2"), cancellationToken);

        return new ClientViewModel(
            Username: username,
            DisplayName: displayName,
            CreatedAt: metadata.CreatedAt,
            Status: metadata.Status,
            Provider: metadata.Provider,
            Password: password,
            Uri: _connectionProfileGenerator.GenerateUri(username, password, displayName),
            ExistsInRuntime: true);
    }
}

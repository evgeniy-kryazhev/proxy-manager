using MediatR;
using ProxyManager.Application.Abstractions;
using ProxyManager.Domain.Clients;

namespace ProxyManager.Application.Clients;

public sealed class GetClientsQueryHandler : IRequestHandler<GetClientsQuery, IReadOnlyCollection<ClientViewModel>>
{
    private readonly IProxyRuntimeConfigGateway _runtimeConfigGateway;
    private readonly IClientMetadataRepository _metadataRepository;
    private readonly IConnectionProfileGenerator _connectionProfileGenerator;

    public GetClientsQueryHandler(
        IProxyRuntimeConfigGateway runtimeConfigGateway,
        IClientMetadataRepository metadataRepository,
        IConnectionProfileGenerator connectionProfileGenerator)
    {
        _runtimeConfigGateway = runtimeConfigGateway;
        _metadataRepository = metadataRepository;
        _connectionProfileGenerator = connectionProfileGenerator;
    }

    public async Task<IReadOnlyCollection<ClientViewModel>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        var runtime = await _runtimeConfigGateway.GetClientsAsync(cancellationToken);
        var metadata = await _metadataRepository.GetAllAsync(cancellationToken);

        var metadataByUsername = metadata.ToDictionary(x => x.Username, StringComparer.OrdinalIgnoreCase);
        var items = new List<ClientViewModel>();

        foreach (var runtimeClient in runtime)
        {
            metadataByUsername.TryGetValue(runtimeClient.Username, out var metadataClient);

            items.Add(new ClientViewModel(
                runtimeClient.Username,
                metadataClient?.DisplayName,
                metadataClient?.CreatedAt ?? DateTimeOffset.MinValue,
                metadataClient?.Status ?? ClientStatus.Active,
                runtimeClient.Provider,
                runtimeClient.Password,
                _connectionProfileGenerator.GenerateUri(runtimeClient.Username, runtimeClient.Password, metadataClient?.DisplayName),
                ExistsInRuntime: true));

            if (metadataClient is not null)
            {
                metadataByUsername.Remove(runtimeClient.Username);
            }
        }

        foreach (var metadataOnly in metadataByUsername.Values)
        {
            items.Add(new ClientViewModel(
                metadataOnly.Username,
                metadataOnly.DisplayName,
                metadataOnly.CreatedAt,
                metadataOnly.Status,
                metadataOnly.Provider,
                Password: "",
                Uri: string.Empty,
                ExistsInRuntime: false));
        }

        return items
            .OrderBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

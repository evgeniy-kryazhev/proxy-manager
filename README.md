# proxy-manager

`proxy-manager` is an MVP self-hosted proxy management system built with ASP.NET Core and Blazor Web App.

## Architecture

The solution is split under `src/` into DDD-oriented layers:

- `ProxyManager.Domain` — entities/value objects/domain rules
- `ProxyManager.Application` — MediatR commands/queries/notifications and infrastructure ports
- `ProxyManager.Infrastructure` — EF Core, SQL Server/SQLite wiring, Identity persistence, Hysteria YAML provider, runtime apply reaction
- `ProxyManager.Web` — Blazor UI that talks directly to Application via mediator

Core request flow:

`Blazor component -> IMediator.Send(...) -> Application handler -> Infrastructure via abstractions`

## MVP features

- Identity-based login for admin access
- Clients list that reconciles runtime Hysteria config + metadata storage
- Create client with generated strong password
- Client details page
- Delete client
- Regenerate client password
- Generated Hysteria URI format:
  - `hysteria2://USERNAME:PASSWORD@excraft.ru:443?sni=excraft.ru&insecure=0#LABEL`

## Runtime assumptions

Default Hysteria settings:

- Config: `/opt/hysteria/config.yaml`
- Compose file: `/opt/hysteria/docker-compose.yml`
- Working directory: `/opt/hysteria`
- Auth mode: `auth.type: userpass`

For local development, `appsettings.Development.json` points to `/tmp/hysteria/config.yaml`.

If Hysteria runs on another host, enable remote runtime access:

- `Hysteria:Remote:Enabled=true`
- `Hysteria:Remote:Host=<server-ip-or-hostname>`
- `Hysteria:Remote:Port=22`
- `Hysteria:Remote:Username=root`
- `Hysteria:Remote:PrivateKeyPath=<path-to-private-key>` if the default SSH identity is not used
- `Hysteria:Remote:KnownHostsPath=<optional-known-hosts-file>` if you do not want to use the default SSH known hosts location

In remote mode the app:

- reads the Hysteria YAML over `ssh user@host "cat /path/to/config.yaml"`
- writes the YAML back over SSH with backup creation as `<config>.bak`
- restarts the runtime with `docker compose restart` on the remote host

The current implementation is non-interactive and expects key-based SSH access.

## Setup

1. Create local Hysteria config for development:

```bash
mkdir -p /tmp/hysteria
cat > /tmp/hysteria/config.yaml <<'YAML'
auth:
  type: userpass
  userpass:
    main: "oldpass123456"
YAML
```

2. Run app:

```bash
dotnet run --project src/ProxyManager.Web/ProxyManager.Web.csproj
```

3. Login with seeded admin account from `appsettings.Development.json`:

- Username: `admin`
- Password: `ChangeMe_12345!`

> ⚠️ The default admin password is for bootstrapping only. Change it before any non-local deployment.
> In production, provide `AdminUser:Password` via secure configuration (environment variables, secret store, etc.), not checked-in files.

## Persistence

- SQL Server is the default configured provider (`Database:Provider=SqlServer`, `ConnectionStrings:DefaultConnection`)
- Development config uses SQLite (`Database:Provider=Sqlite`, `Data Source=proxy-manager.db`) for local portability
- EF Core stores Identity and client metadata (`ClientMetadata`)
- Startup applies EF Core migrations (`Database.Migrate()`) instead of `EnsureCreated()`

## Extension points

- `IProxyRuntimeConfigGateway` — provider runtime config access/mutation
- `IConnectionProfileGenerator` — provider URI generation
- `IPasswordGenerator` — password generation strategy
- `ProxyConfigurationChangedNotification` — event-driven reaction entrypoint

Hysteria is the first provider implementation; additional providers can be introduced by adding new infrastructure implementations for the same application contracts.

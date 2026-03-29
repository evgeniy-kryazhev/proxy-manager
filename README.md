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

## Setup

1. Create local Hysteria config for development:

```bash
mkdir -p /tmp/hysteria
cat > /tmp/hysteria/config.yaml <<'YAML'
auth:
  type: userpass
  userpass:
    - username: existing
      password: oldpass123456
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

- SQL Server is the default configured provider (`ConnectionStrings:SqlServer` in `appsettings.json`)
- Development config uses SQLite (`Data Source=proxy-manager.db`) for local portability
- EF Core stores Identity and client metadata (`ClientMetadata`)

## Extension points

- `IProxyRuntimeConfigGateway` — provider runtime config access/mutation
- `IConnectionProfileGenerator` — provider URI generation
- `IPasswordGenerator` — password generation strategy
- `ProxyConfigurationChangedNotification` — event-driven reaction entrypoint

Hysteria is the first provider implementation; additional providers can be introduced by adding new infrastructure implementations for the same application contracts.

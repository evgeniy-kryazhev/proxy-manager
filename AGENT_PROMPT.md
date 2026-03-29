# ProxyManager — Master Agent Prompt

Use this prompt as the primary implementation brief for the repository.

---

## Mission

Build an MVP of a self-hosted proxy management system called `proxy-manager` using ASP.NET Core and Blazor Web App.

This MVP must be implemented with a clean, scalable, domain-driven architecture. The goal is **not** to produce a quick throwaway admin panel, but to create a maintainable foundation that can evolve without major redesign.

The first supported proxy provider is **Hysteria 2**, but the system must be architected so that Hysteria is only the **first provider implementation**, not the permanent center of the application.

---

## Core principles

1. **MVP in features, not in architecture**  
   Keep the feature set small, but the architecture clean and extensible.

2. **DDD-oriented structure**  
   The project must use clear boundaries between Domain, Application, Infrastructure, and Web.

3. **Blazor is a presentation layer, not an internal API client**  
   Blazor pages/components must call the Application layer directly through mediator. Do not build this as “Blazor frontend calling its own internal REST API” for core features.

4. **Mediator is mandatory**  
   Use MediatR (or an equivalent mediator library) to model commands, queries, and notifications.

5. **Identity is shared**  
   Use ASP.NET Core Identity as the single identity system. Blazor and any future API must share one identity core, even if their authentication flows differ.

6. **Persistence is Infrastructure**  
   Use EF Core in Infrastructure. Use SQL Server as the current/default provider, while keeping the architecture provider-agnostic at the Domain/Application level.

7. **Proxy providers must be replaceable**  
   Hysteria-specific logic must be isolated. The application must allow replacing Hysteria or adding another proxy provider later with minimal cost.

8. **Runtime reconfiguration reactions must be event-driven and replaceable**  
   When active proxy config changes, publish an event/notification. Do not hardcode restart logic inside core use cases.

---

## Required solution structure

Create a multi-project solution with these projects:

- `ProxyManager.Domain`
- `ProxyManager.Application`
- `ProxyManager.Infrastructure`
- `ProxyManager.Web`

### Domain responsibilities
- Pure business model
- Entities, value objects, domain rules
- No EF Core
- No ASP.NET Identity
- No YAML parsing
- No Docker/system calls
- No Blazor/UI concerns

### Application responsibilities
- Commands, queries, notifications/events
- Use cases handled through mediator
- Interfaces/ports for infrastructure dependencies
- DTOs/result models
- Application orchestration
- No concrete infrastructure logic

### Infrastructure responsibilities
- EF Core and SQL Server integration
- ASP.NET Identity persistence and configuration
- Hysteria runtime config access and mutation
- YAML parsing/writing
- Restart/reload handlers
- File/process/system integrations
- Concrete provider implementations

### Web responsibilities
- Blazor Web App UI
- Authentication UX
- Authorization
- Presentation/view models
- Direct mediator usage through DI
- No internal HTTP API dependency for core app behavior

---

## Architecture rules

### 1. Blazor must not use an internal API for core app behavior

Blazor pages/components must call the Application layer directly.

Expected flow:

`Blazor component -> IMediator.Send(...) -> Application handler -> Domain/Infrastructure abstractions`

Not this:

`Blazor component -> HttpClient -> internal controller/api -> service`

If a future REST API is added, it must reuse the same Application layer.

### 2. Mediator must be central to application behavior

Use mediator for all meaningful use cases.

Examples:

#### Commands
- `CreateClientCommand`
- `DeleteClientCommand`
- `RegenerateClientPasswordCommand`

#### Queries
- `GetClientsQuery`
- `GetClientDetailsQuery`

#### Notifications / Events
- `ProxyConfigurationChangedNotification`
- or a similarly clear name

Blazor components must call mediator, not repositories or infrastructure directly.

### 3. Identity must be shared and future-proof

Use ASP.NET Core Identity as the only identity/auth foundation.

Requirements:
- Identity lives in Infrastructure
- Blazor uses cookie-based auth
- Future API must be able to use token/bearer auth while reusing the same Identity user system
- Do not create separate auth systems for Blazor and API
- Domain must not depend on ASP.NET Identity types directly

### 4. EF Core and SQL Server usage

Use EF Core in Infrastructure with SQL Server as the current/default provider.

Requirements:
- Domain and Application must not depend on SQL Server-specific logic
- SQL Server is the initial provider, but architecture must not be tightly coupled to it
- Provider-specific details must remain in Infrastructure
- Use EF Core for:
  - Identity
  - metadata
  - internal state
  - optional audit/history if useful

Important:
Hysteria runtime config remains an **external runtime source**, not the primary EF model.

### 5. Proxy provider extensibility is a hard requirement

Do not architect the system as a Hysteria-only manager.

Instead:
- Model the core as proxy management
- Treat Hysteria as the first provider implementation
- Isolate Hysteria-specific logic behind abstractions
- Keep provider-specific URI generation, config mutation, and apply/restart behavior out of the core domain

For MVP, implement only Hysteria, but do not make Hysteria a global architectural assumption.

### 6. Config-changed handling must be replaceable

When active proxy credentials/config are changed:
- publish a notification/event
- let infrastructure react to it
- keep the reaction swappable

Default implementation for Hysteria may run:

`docker compose restart` in `/opt/hysteria`

But the core use case must not hardcode Docker behavior directly.

This must be easy to replace later with:
- systemd restart
- reload action
- delayed apply
- queue-based worker
- webhook
- different provider-specific behavior

An in-process mediator notification is enough for MVP.

---

## Domain direction

Do not model the domain around raw Hysteria YAML.

Model it around proxy/client management concepts.

Suggested concepts:
- `ProxyClient`
- `ProxyProvider`
- `ConnectionProfile`
- `ClientUsername` (value object)
- `ClientPassword` (value object)
- `ClientDisplayName` (value object)
- `ClientStatus`

Keep the domain meaningful, but do not overengineer it.

Do not introduce useless patterns just to look “DDD”.

---

## Suggested application boundaries

Suggested interfaces/ports (names may vary if improved):

- `IProxyProvider`
- `IProxyProviderRegistry`
- `IProxyClientProvisioningService`
- `IProxyRuntimeConfigGateway`
- `IClientMetadataRepository`
- `IConnectionProfileGenerator`
- `IConfigChangeReaction`
- `ICurrentUserService`

These are examples, not mandatory exact names. The important part is the boundary design.

---

## Hysteria MVP scope

Implement the first provider for **Hysteria 2**.

### Environment assumptions
- Hysteria config path: `/opt/hysteria/config.yaml`
- Docker Compose path: `/opt/hysteria/docker-compose.yml`
- Working directory: `/opt/hysteria`
- Current auth mode: `auth.type: userpass`
- Existing TLS/domain settings already exist and must not be modified by the app

### Default connection settings
- host: `excraft.ru`
- port: `443`
- sni: `excraft.ru`

### Generated URI format
`hysteria2://USERNAME:PASSWORD@excraft.ru:443?sni=excraft.ru&insecure=0#LABEL`

---

## Source of truth model

For active Hysteria credentials:

- `/opt/hysteria/config.yaml` is the runtime source of truth

Metadata is supplemental and stored through EF Core / SQL Server.

This means:
- if a client exists in Hysteria runtime config but not in metadata, it must still be shown in the UI
- reads must reconcile runtime users with metadata
- metadata must not be treated as the only truth for active access

---

## Required MVP features

### 1. Login / protected admin access
- Use ASP.NET Core Identity
- Blazor uses cookie authentication
- Admin pages must be protected

### 2. Clients list page
Show existing clients for the active provider.

At minimum show:
- username
- display name
- created at
- status
- provider
- generated URI
- available actions

### 3. Create client
- Input username
- Optional display name
- Generate a strong random password
- Add client to Hysteria `auth.userpass`
- Persist metadata in SQL Server
- Generate import URI
- Publish config-changed notification
- Trigger provider-specific apply/restart reaction through the notification mechanism
- Show generated connection data after creation

### 4. Client details page
Show:
- username
- display name
- password
- URI
- copy actions

### 5. Delete client
- Confirm before deletion
- Remove from runtime provider config
- Update metadata appropriately
- Publish config-changed notification

### 6. Regenerate password
- Confirm before action
- Generate a new strong password
- Update runtime config
- Update metadata
- Regenerate URI
- Publish config-changed notification

---

## Infrastructure requirements

### 1. YAML handling
- Use proper YAML parsing/writing
- Do not use brittle string replacements as the main solution
- Preserve unrelated configuration fields
- Only mutate the relevant Hysteria userpass/auth area

### 2. SQL Server persistence
Use EF Core + SQL Server for:
- Identity
- metadata
- internal app state
- optional audit/history if useful

Architecture must remain clean enough that a provider switch later should mainly affect Infrastructure/configuration.

### 3. Restart/apply behavior
Default Hysteria implementation:
- run `docker compose restart` in `/opt/hysteria`
- capture success/failure
- surface useful feedback

But keep this behind a replaceable abstraction triggered by a notification/event.

---

## UI expectations

This is an internal tool.

Priorities:
- clarity
- correctness
- maintainability
- simplicity

Do not overinvest in flashy UI.
Do not put business logic in Razor components.
Do not let Razor components talk directly to infrastructure.

Suggested pages:
- Login
- Clients list
- Create client
- Client details

---

## Implementation constraints

### Must do
- Use mediator
- Use DDD-oriented layering
- Use ASP.NET Core Identity
- Use EF Core with SQL Server
- Keep Blazor as presentation over Application
- Keep Hysteria as first provider, not permanent core assumption
- Keep config-changed behavior event-driven and replaceable

### Must not do
- Do not make Blazor call an internal API for core operations
- Do not put business logic in Razor components
- Do not directly call Docker from UI
- Do not tightly couple Domain/Application to Hysteria YAML structure
- Do not tightly couple Domain/Application to SQL Server specifics
- Do not build a fake plugin framework with excessive complexity
- Do not introduce pattern cargo-culting

---

## Suggested delivery approach

Implement incrementally in small, coherent steps.

Recommended order:
1. Solution structure and references
2. Domain/Application contracts and mediator setup
3. Infrastructure baseline (EF Core, SQL Server, Identity)
4. Provider abstraction + Hysteria provider
5. Client list/read flow
6. Create client flow
7. Delete client flow
8. Password regeneration flow
9. Final polish and README

Prefer working, reviewable progress over speculative overengineering.

---

## Deliverables

- Working solution named `proxy-manager`
- Clean multi-project structure
- ASP.NET Core + Blazor Web App
- Mediator-based Application layer
- Shared ASP.NET Identity integration
- EF Core + SQL Server persistence
- Hysteria provider implementation
- Replaceable config-changed reaction mechanism
- README with:
  - setup instructions
  - architecture overview
  - explanation of extension points

---

## Definition of done

The task is done when:

- the solution builds successfully
- the project is split into Domain / Application / Infrastructure / Web
- Blazor uses mediator directly, not internal API calls
- Identity works for Blazor login
- Hysteria users can be read from `/opt/hysteria/config.yaml`
- new clients can be created
- passwords can be regenerated
- clients can be deleted
- URIs are generated correctly
- metadata persists via EF Core/SQL Server
- config changes trigger restart/apply through a replaceable event-driven mechanism
- the architecture clearly leaves room for adding or replacing proxy providers later with minimal redesign

---

## Final note

This project should feel like a real internal platform foundation, not a quick script with a UI attached.

Be pragmatic, but disciplined.
Favor clean boundaries, explicit use cases, and maintainable evolution.

# Blazor CRM (ASP.NET Core / .NET 9)

A compact but complete CRM sample built with Blazor Server, EF Core (PostgreSQL), ASP.NET Core Identity, JWT, SignalR, Quartz, OpenTelemetry, and a layered architecture.

The solution demonstrates real-world practices: CQRS via MediatR, multi-tenancy, caching, rate limiting, telemetry, and background jobs.

## Solution layout
- src/Crm.Domain — domain models and enums
- src/Crm.Application — application layer (CQRS/MediatR, interfaces, validation, permissions)
- src/Crm.Infrastructure — infrastructure (EF Core DbContext, migrations, Identity, services, Quartz, SignalR hub)
- src/Presentation/Crm.Web — Blazor Server app + minimal APIs
- src/Presentation/Crm.UI — shared Blazor UI components
- src/Background/Crm.Worker — .NET Worker (BackgroundService)
- tests/* — test projects

## Features
- Blazor Server UI: Companies, Contacts, Deals, Tasks, Activities, Dashboard and details pages
- EF Core + PostgreSQL; migrations applied on startup (Database.Migrate)
- ASP.NET Core Identity + roles (Admin/Manager/User), Cookie login (web) and JWT (API)
- Multi-tenancy via TenantId + global EF filters (claim "tenant")
- MediatR pipeline behavior for permissions (PermissionBehavior)
- SignalR NotificationsHub for realtime notifications
- Quartz scheduled jobs (RemindersSweepJob every 5 min) with persistent store (PostgreSQL)
- Output caching, Response compression, CORS, Rate limiting, Health checks
- OpenTelemetry (traces/metrics) with OTLP exporter
- File storage (IAttachmentService, LocalFileStorage) + download endpoint

## Screenshots
![Blazor CRM Screenshot](https://www.unrealbg.com/blazorcrm/blazorcrm.png)

## Tech stack
- .NET 9, ASP.NET Core, Blazor Server
- Entity Framework Core (Npgsql)
- ASP.NET Core Identity, JWT Bearer, Policy scheme (Cookie/JWT)
- MediatR, Quartz.NET, SignalR
- OpenTelemetry (OTLP)

## Quick start
### Prerequisites
- .NET 9 SDK
- PostgreSQL database (local or container)

### Configuration (appsettings.*)
- ConnectionStrings:DefaultConnection — Npgsql connection string
- Jwt:Key, Jwt:Issuer, Jwt:Audience — JWT settings
- Cors:AllowedOrigins — CORS origins for policy "maui"
- Seed:AdminEmail / Seed:AdminPassword / Seed:AdminRoles — initial user and roles
- Quartz:SchemaSqlPath — optional path to the Quartz SQL schema script

Example (appsettings.Development.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=blazor_crm;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "dev-key-please-set",
    "Issuer": "BlazorCrm",
    "Audience": "BlazorCrm"
  },
  "Cors": { "AllowedOrigins": ["https://localhost:5001", "http://localhost:5000"] },
  "Seed": {
    "AdminEmail": "admin@local",
    "AdminPassword": "Admin123$",
    "AdminRoles": ["Admin"]
  }
}
```

Notes:
- In Development, if the connection string is missing, a dev fallback to local PostgreSQL is used.
- On startup: EF migrations are applied, IdentitySeeder runs (roles/admin), DemoDataSeeder runs in Development only.
- Data Protection keys are stored in the database.
- Demo data seeding can be toggled with Seed:DemoData (true/false). In Development it defaults to true via appsettings.Development.json.

### Quartz schema (PostgreSQL)
Quartz uses a persistent store. On first startup the app checks for Quartz tables and, if missing, looks for a SQL script and applies it automatically.
Place the official Quartz PostgreSQL SQL script at one of:
- <contentRoot>/sql/quartz_postgres.sql
- <contentRoot>/quartz_postgres.sql
- or point to it via configuration: Quartz:SchemaSqlPath

### Run
- Web (Blazor + API):
  - `dotnet run --project src/Presentation/Crm.Web/Crm.Web.csproj`
  - Open https://localhost:<port>
- Worker (optional):
  - `dotnet run --project src/Background/Crm.Worker/Crm.Worker.csproj`

Default login (after seed):
- Email: admin@local
- Password: Admin123$

## Database and migrations
- DbContext: Crm.Infrastructure.Persistence.CrmDbContext
- Entities: Tenant, Company, Contact, Pipeline, Stage, Deal, Activity, TaskItem, Attachment, Team, UserTeam, RefreshToken, AuditEntry
- Company/Contact have Tags (List<string>) with a custom ValueComparer and string conversion
- Global TenantId filters applied to most tables
- Useful indexes for common queries (e.g., Deal: StageId/OwnerId/CompanyId/ContactId; Activity/TaskItem: RelatedId; etc.)
- Unified search uses PostgreSQL FTS with stored tsvector columns and GIN indexes. Migration 20260120000000_AddSearchVectors adds unaccent/pg_trgm extensions.

Manual EF commands:
- Add migration: `dotnet ef migrations add <Name> -p src/Crm.Infrastructure -s src/Presentation/Crm.Web`
- Update DB: `dotnet ef database update -p src/Crm.Infrastructure -s src/Presentation/Crm.Web`

## Security and permissions
- Authentication: Cookie (web) and JWT (API). A policy scheme chooses based on Authorization header.
- Policies/Permissions: see Crm.Application.Security.Permissions and Program.cs
- Roles: Admin/Manager/User (seeded)
- Multi-tenancy: ITenantProvider (HttpTenantProvider) reads claim "tenant"; defaults to Guid.Empty when missing
- Antiforgery: HTML form endpoints (/auth/login and /auth/logout) validate antiforgery tokens. The Blazor forms include <AntiforgeryToken /> inside the form body (not in <head>).

## API (v1) — endpoints and examples
All /api routes are protected and use CORS policy "maui" and fixed-window rate limiting (60 req/min). Use Bearer <accessToken> for protected routes.

Auth
- POST /api/auth/login — issue JWT + refresh token
- POST /api/auth/refresh — rotate refresh token
- POST /api/auth/logout — revoke refresh token(s)

Example: login (JWT)
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "admin@local",
    "password": "Admin123$"
  }'
```
Response:
```json
{
  "accessToken": "eyJhbGciOi...",
  "refreshToken": "6cfd...",
  "expiresIn": 3600
}
```

Example: refresh
```bash
curl -X POST https://localhost:5001/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken": "<refreshToken>"}'
```

Example: logout (revoke a single refresh token)
```bash
curl -X POST https://localhost:5001/api/auth/logout \
  -H "Authorization": "Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d '{"refreshToken": "<refreshToken>"}'
```

Companies
- GET /api/companies — search/filter/sort/pagination
- POST /api/companies — create
- PUT /api/companies/{id} — update
- DELETE /api/companies/{id} — delete
- GET /api/companies/industries — distinct industries

Example: list companies with filter/sort/paging
```bash
curl "https://localhost:5001/api/companies?search=soft&industry=SaaS&sort=Name&asc=true&page=1&pageSize=10" \
  -H "Authorization: Bearer <accessToken>"
```
Response:
```json
{
  "items": [
    { "id": "...", "name": "Acme", "industry": "SaaS", "tags": ["key"], "address": "...", "tenantId": "..." }
  ],
  "total": 1
}
```

Example: create company
```bash
curl -X POST https://localhost:5001/api/companies \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Contoso",
    "industry": "Manufacturing",
    "tags": ["partner","priority"]
  }'
```
Response: `"<new-company-guid>"`

Example: update company
```bash
curl -X PUT https://localhost:5001/api/companies/<id> \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d '{
    "id": "<id>",
    "name": "Contoso Ltd",
    "industry": "Manufacturing",
    "tags": ["partner"]
  }'
```

Contacts
- POST /api/contacts, PUT /api/contacts/{id}, DELETE /api/contacts/{id}

Deals
- POST /api/deals, PUT /api/deals/{id}, DELETE /api/deals/{id}

Activities
- POST /api/activities, PUT /api/activities/{id}, DELETE /api/activities/{id}

Tasks
- POST /api/tasks, PUT /api/tasks/{id}, DELETE /api/tasks/{id}

Attachments
- GET /attachments/{id} — download attachment

Example: download attachment
```bash
curl -L "https://localhost:5001/attachments/<id>" \
  -H "Authorization: Bearer <accessToken>" -o file.bin
```

Health
- GET /health/live, GET /health/ready

Form login (web)
- POST /auth/login — Cookie sign-in for Blazor UI form

## Telemetry
- OpenTelemetry tracing and metrics (ASP.NET Core, HttpClient, EF Core) with OTLP exporter.
- Configure via standard env vars, e.g. `OTEL_EXPORTER_OTLP_ENDPOINT`.

## Files and attachments
- IAttachmentService with LocalFileStorage. Download: GET /attachments/{id}.

## UI and safety
- Blazor components in src/Presentation/Crm.Web/Components and src/Presentation/Crm.UI
- Tightened CSP (no inline scripts), static assets from wwwroot
- Compression and output cache enabled

## Testing
- `dotnet test` (tests/*)

## Troubleshooting
- Missing Quartz tables: add/point to quartz_postgres.sql (see Quartz schema above)
- Missing Jwt:Key in Production: set a value (otherwise startup throws InvalidOperationException)
- DB connection: set ConnectionStrings:DefaultConnection

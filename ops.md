# Ops notes

## Database migrations

### Recommended production workflow

1. Apply migrations out-of-band:
   - `dotnet ef database update -p src/Crm.Infrastructure -s src/Presentation/Crm.Web`
2. Keep `Database:AutoMigrate` set to `false` in production.

### Auto-migrate behavior

- Development/Test: auto-migrate enabled by default.
- Production: auto-migrate is disabled by default. To enable it explicitly, set:
  - `Database:AutoMigrate: true`

If the schema is missing or out of date in production and auto-migrate is disabled, the app fails fast with a clear error and the readiness check will be unhealthy.

### Optional migration runner mode

If you want a dedicated run mode, use a startup command or job that only runs:

- `dotnet ef database update -p src/Crm.Infrastructure -s src/Presentation/Crm.Web`

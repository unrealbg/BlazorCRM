# Ops notes

## Database migrations

### Recommended production workflow

1. Apply migrations out-of-band (recommended):
  - `dotnet Crm.Web.dll --migrate`
2. Start the app normally after migrations complete.
3. Keep `Database:AutoMigrate` set to `false` in production.

### Auto-migrate behavior

- Development/Test: auto-migrate enabled by default.
- Production: auto-migrate is disabled by default. To enable it explicitly, set:
  - `Database:AutoMigrate: true`

If the schema is missing or out of date in production and auto-migrate is disabled, the app still starts but the readiness check will be unhealthy.

### Migration runner mode

Run a one-off migration step during deploy:

- `dotnet Crm.Web.dll --migrate`

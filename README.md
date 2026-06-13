# Cinema Network

Web application for managing a cinema network: public catalog, online seat booking with PayPal / Stripe payments, QR tickets, customer loyalty, admin console, and cashier panel.

**Stack:** ASP.NET 8 · Angular 19 + Material · MS SQL Server (Docker) · EN + UK localization.

## Requirements

- .NET SDK 8.x
- Node.js 20+ (tested on 24)
- Docker Desktop
- Git

## Quick start

```bash
git clone <this-repo> cinema && cd cinema
docker compose up -d                      # MS SQL Server on localhost:1443
dotnet restore backend/Cinema.sln
npm --prefix frontend ci
```

Create a local `.env` file first if you do not have one:

```bash
cp .env.example .env
```

The repository tracks only `backend/Cinema.Api/appsettings.json`. Environment-specific appsettings files such as `appsettings.Development.json`, `appsettings.Testing.json`, and local `.env` files are ignored by git.

### Run backend

```bash
dotnet run --project backend/Cinema.Api
# http://localhost:5000  (Swagger at /swagger in Development)
```

### Run frontend

```bash
npm --prefix frontend start
# http://localhost:4200
```

## Production deployment

### Prerequisites
- Docker Engine 24+ with Compose v2
- A `.env` file at the project root (copy from `.env.example`)

### 1. Create `.env` file

Copy `.env.example` to `.env` and fill in real values:

```dotenv
MSSQL_SA_PASSWORD=<strong-password>
JWT_KEY=<at-least-32-character-secret>
STRIPE_SECRET_KEY=
STRIPE_WEBHOOK_SECRET=
PAYPAL_CLIENT_ID=
PAYPAL_CLIENT_SECRET=
PAYPAL_WEBHOOK_ID=
EMAIL_HOST=
EMAIL_PORT=587
EMAIL_USER=
EMAIL_PASSWORD=
ALLOWED_ORIGIN=http://your-domain.com
```

Do not commit `.env`; it is intentionally ignored.

### 2. Build and start

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

This starts:
- **mssql** - MS SQL Server with persistent volume
- **backend** - ASP.NET 8 API (port 8080 internally)
- **frontend** - Angular SPA served by nginx (port 80 internally)
- **nginx** - Reverse proxy on port 80; routes `/api/*` to backend, `/*` to frontend

### 3. Migrations run automatically on first boot

In `Production` mode the backend does NOT auto-migrate. Run migrations manually once:

```bash
docker compose -f docker-compose.prod.yml exec backend \
  dotnet ef database update \
  --project /app/Cinema.Infrastructure.dll \
  -- --connection "Server=mssql;Database=Cinema;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True"
```

> **Tip:** For a quick seed of admin/cashier users you can temporarily set `ASPNETCORE_ENVIRONMENT=Development`, restart, then set it back to `Production`.

### 4. Stop

```bash
docker compose -f docker-compose.prod.yml down
```

## Tests

```bash
dotnet test backend/Cinema.sln            # backend unit + integration
npm --prefix frontend run test:ci          # frontend unit (headless)
```

Backend integration tests use Testcontainers to start a temporary SQL Server Docker container. It is stopped when the test factory is disposed, but if the test process is interrupted, Docker Desktop may still show an orphaned test container.

Run only backend unit tests when you do not want Docker containers:

```bash
dotnet test backend/Cinema.Tests/Cinema.Tests.csproj --filter "FullyQualifiedName~Cinema.Tests.Unit"
```

Collect backend test coverage:

```bash
dotnet test backend/Cinema.Tests/Cinema.Tests.csproj --collect:"XPlat Code Coverage"
```

## Project layout

```
backend/                  ASP.NET solution
├─ Cinema.Api             Web API, DI, JWT, Swagger
├─ Cinema.Application     use-cases, DTO, validators
├─ Cinema.Domain          entities, enums, domain logic
├─ Cinema.Infrastructure  EF Core, Stripe/PayPal, email, QR
└─ Cinema.Tests           xUnit + Testcontainers

frontend/                 Angular 19 SPA
├─ src/app/core           auth, interceptors, guards
├─ src/app/shared         reusable components
├─ src/app/features       catalog / booking / account / admin / cashier
└─ public/i18n            en.json, uk.json

docker-compose.yml         dev (MSSQL only)
docker-compose.prod.yml    prod (mssql + backend + frontend + nginx)
```

## License

TBD

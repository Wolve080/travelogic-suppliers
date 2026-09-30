# Travelogic Suppliers

In tourism, **suppliers** provide the services the industry is built on: a hotel provides overnight accommodation, and a safari operator provides a half day game drive (an *activity*). This solution manages those suppliers and their services.

It consists of:

* **Supplier service**: an ASP.NET Core 10 Web API that owns supplier data in SQL Server. It's built as an independent microservice with its own database schema, versioned contract, container image, CI pipeline, health probes, telemetry and integration events.
* **Web app**: a React 19 + TypeScript single page app. The home screen lists all suppliers, and a form adds a supplier **together with its services**. You can also view, edit and delete suppliers and manage individual services.

![Stack](https://img.shields.io/badge/.NET-10-512BD4) ![Stack](https://img.shields.io/badge/React-19-61DAFB) ![Stack](https://img.shields.io/badge/SQL%20Server-2022-CC2927) ![Stack](https://img.shields.io/badge/Docker-compose-2496ED)

> For the reasoning behind the design, see **[docs/architecture.md](docs/architecture.md)**.

---

## Quick start

### One click

| OS | Start | Stop |
| --- | --- | --- |
| Windows | double-click **`start.cmd`** | `stop.cmd` |
| macOS | double-click **`start.command`** | `stop.command` |
| Linux | `./start.command` | `./stop.command` |

The script checks that Docker is installed (on Windows it offers to install Docker Desktop with winget), starts Docker Desktop if it isn't running, builds and starts everything, waits until the app responds and opens <http://localhost:3000> in your browser.

### Manually

The only prerequisite is **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** (or Docker Engine with the Compose plugin).

```bash
git clone https://github.com/Wolve080/travelogic-suppliers.git travelogic
cd travelogic
docker compose up --build
```

The first run downloads the base images and takes a few minutes. When the logs settle, open:

| What | URL |
| --- | --- |
| **Web app** | <http://localhost:3000> |
| API reference (Scalar, try requests in the browser) | <http://localhost:5080/scalar> |
| OpenAPI document | <http://localhost:5080/openapi/v1.json> |
| Readiness probe | <http://localhost:5080/health/ready> |

On first start the API creates the database, applies migrations and seeds three example suppliers, so the home screen isn't empty.

To stop, press `Ctrl+C` and run `docker compose down`. Add `-v` to also delete the database volume.

> **Port already in use?** The stack uses ports 3000, 5080 and 1433. Stop whatever is using them, or change the left-hand port numbers in `docker-compose.yml`.
>
> **Apple Silicon:** SQL Server images are x64 only. Enable *"Use Rosetta for x86/amd64 emulation"* in Docker Desktop's settings.

---

## Running for development

This runs SQL Server in Docker, and the API and web app on your machine with hot reload.

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download), [Node.js 24+](https://nodejs.org), Docker.

```bash
# 1. Database
docker compose up -d sqlserver

# 2. API: http://localhost:5080  (API reference at /scalar)
cd services/supplier
dotnet run --project src/Travelogic.Suppliers.Api

# 3. Web app, in a second terminal: http://localhost:5173
cd web
npm install
npm run dev
```

In `Development` the API migrates and seeds the database on startup (see `appsettings.Development.json`). The Vite dev server proxies `/api` to `http://localhost:5080`, so no CORS setup is needed.

If you have SQL Server installed locally, you can use it instead of Docker. Point the connection string at it without editing files:

```bash
cd services/supplier/src/Travelogic.Suppliers.Api
dotnet user-secrets set "ConnectionStrings:SuppliersDb" "Server=.;Database=TravelogicSuppliers;Trusted_Connection=True;TrustServerCertificate=True"
```

---

## Running the tests

```bash
# Back end: unit tests plus integration tests against a real SQL Server
# (started automatically with Testcontainers, so Docker must be running)
cd services/supplier
dotnet test

# Front end: lint, type check, unit and component tests
cd web
npm run lint && npm run typecheck && npm test
```

| Suite | Count | Covers |
| --- | --- | --- |
| Domain + application unit tests | 38 | Business rules, validation, use-case error handling |
| API integration tests | 15 | Real HTTP pipeline + real SQL Server: status codes, problem details, search/paging, cascade delete, optimistic concurrency, outbox publishing |
| Front-end tests | 12 | Form schemas, request mapping, server error mapping, list/search and create flows |

GitHub Actions runs both suites on every push and pull request (`.github/workflows`). Each deployable has its own workflow and runs only when its code changes.

---

## Repository layout

```
.
├── docker-compose.yml             # SQL Server + Supplier service + web app
├── start.cmd / start.command      # one-click start (Windows / macOS, Linux)
├── scripts/start.ps1              # what start.cmd runs
├── docs/architecture.md           # Design decisions (start here for the walkthrough)
├── services/
│   └── supplier/                  # Self-contained microservice; could live in its own repo
│       ├── Dockerfile
│       ├── Directory.Build.props  # Shared build settings, analyzers, warnings as errors
│       ├── Directory.Packages.props  # Central package versions
│       ├── src/
│       │   ├── Travelogic.Suppliers.Domain/          # Aggregate, value objects, domain events. No dependencies.
│       │   ├── Travelogic.Suppliers.Application/     # Use cases, contracts, validators, ports
│       │   ├── Travelogic.Suppliers.Infrastructure/  # EF Core, SQL Server, migrations, outbox
│       │   └── Travelogic.Suppliers.Api/             # Controllers, problem details, versioning, OpenAPI, telemetry
│       └── tests/
│           ├── Travelogic.Suppliers.UnitTests/
│           └── Travelogic.Suppliers.IntegrationTests/  # WebApplicationFactory + Testcontainers
└── web/                           # React + TypeScript + Vite
    ├── Dockerfile, nginx.conf     # Static hosting + reverse proxy to the API
    └── src/
        ├── api/                   # Typed client, TanStack Query hooks
        ├── components/            # Small UI kit (buttons, fields, dialog, pagination)
        └── features/suppliers/    # Pages, forms, schemas
```

---

## API at a glance

All endpoints are under `/api/v1`. Errors are returned as [RFC 9457 problem details](https://www.rfc-editor.org/rfc/rfc9457), with a stable `code` and a `correlationId`.

| Method | Route | |
| --- | --- | --- |
| `GET` | `/suppliers?search=&type=&isActive=&sortBy=Name\|CreatedAt\|City&descending=&page=&pageSize=` | List suppliers (paged) |
| `POST` | `/suppliers` | Create a supplier with its services |
| `GET` | `/suppliers/{id}` | Get a supplier, its services and its `version` |
| `PUT` | `/suppliers/{id}` | Update details (send back `version`; `409` if someone else changed it) |
| `DELETE` | `/suppliers/{id}` | Delete a supplier and its services |
| `GET` `POST` | `/suppliers/{id}/services` | List / add services |
| `GET` `PUT` `DELETE` | `/suppliers/{id}/services/{serviceId}` | Read / update / remove a service |
| `GET` | `/reference-data` | Supplier types, service categories and pricing units |

Example (see [`Travelogic.Suppliers.Api.http`](services/supplier/src/Travelogic.Suppliers.Api/Travelogic.Suppliers.Api.http) for more):

```http
POST /api/v1/suppliers
Content-Type: application/json

{
  "name": "Garden Route Adventures",
  "type": "ActivityProvider",
  "contact": { "email": "hello@gardenroute.example" },
  "address": { "city": "Knysna", "region": "Western Cape", "country": "South Africa" },
  "services": [
    { "name": "Half Day Kayak Tour", "category": "Activity", "price": 650, "currency": "ZAR",
      "pricingUnit": "PerPerson", "durationMinutes": 240, "capacity": 12 }
  ]
}
```

---

## Configuration

Set values in `appsettings*.json`, or as environment variables using `__` for nesting (e.g. `Database__MigrateOnStartup=true`).

| Setting | Default | Purpose |
| --- | --- | --- |
| `ConnectionStrings:SuppliersDb` | *(required)* | SQL Server connection string |
| `Database:MigrateOnStartup` | `false` (`true` in Development/Docker) | Apply EF Core migrations at startup |
| `Database:SeedSampleData` | `false` (`true` in Development/Docker) | Insert example suppliers into an empty database |
| `Outbox:Enabled` / `PollingInterval` / `BatchSize` / `MaxAttempts` | `true` / `5s` / `50` / `10` | Integration event publishing |
| `OpenApi:Enabled` | `true` in Development | Serve `/openapi/v1.json` and `/scalar` |
| `Cors:AllowedOrigins` | `[]` | Origins allowed to call the API directly (not needed behind the proxy) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | *(unset)* | Export traces and metrics over OTLP, e.g. to the .NET Aspire dashboard |
| `SQL_SA_PASSWORD` (compose only) | `Travelogic!Passw0rd` | SA password for the local SQL Server container. Override it in a `.env` file (see `.env.example`). |

The default credentials are for local development only. A real deployment would take them from a secret store.

---

## Highlights

* **Clean Architecture + DDD:** `Supplier` is an aggregate root that enforces its own rules (such as unique service names per supplier). Value objects cover `Money`, `Address` and `ContactDetails`.
* **Transactional outbox:** domain events are saved in the same transaction as the change, then published in order by a background processor that is safe to run on several instances at once.
* **Optimistic concurrency across the whole aggregate:** a SQL `rowversion`, bumped by changes to services as well.
* **Problem details everywhere,** with field-level errors (`services[0].price`) that the UI maps back onto the right input.
* **Operability:** liveness/readiness probes, JSON structured logs, correlation ids, OpenTelemetry, a non-root container, forwarded headers.
* **Tested against real SQL Server** via Testcontainers, not an in-memory fake.
* **Front end:** URL-driven search/filter/sort/paging, a dynamic services list in the create form, inline service editing, conflict handling, accessible forms and dialogs, route-level code splitting.

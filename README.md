# CleanAI Cleaning Service Backend

![.NET Core](https://img.shields.io/badge/.NET%208.0-Purple?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?logo=swagger&logoColor=black)

ASP.NET Core API for a cleaning-service marketplace connecting clients, workers, and administrators. The backend uses PostgreSQL, Entity Framework Core, JWT authentication, and an optional Ollama-powered assistant.

## Prerequisites

Install the following before starting:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- EF Core CLI 9.0.17:

```powershell
dotnet tool install --global dotnet-ef --version 9.0.17
```

If `dotnet-ef` is already installed with a different version:

```powershell
dotnet tool update --global dotnet-ef --version 9.0.17
```

## Project Structure

```text
CleaningServiceApp/
|- CleaningService.API/   ASP.NET Core controllers and application startup
|- BLL/                   DTOs, interfaces, and business services
|- DAL/                   EF Core context, entities, repositories, and migrations
|- docker-compose.yml     PostgreSQL, API, and Ollama services
`- CleaningServiceApp.sln
```

Run the commands below from `CleaningServiceApp`:

```powershell
cd .\CleaningServiceApp
```

If you are at the parent workspace containing both repositories, use:

```powershell
cd .\PRM393_Cleaning\CleaningServiceApp
```

## 1. Configure Docker

Create the local Compose environment file:


Update `.env` with local values:

```dotenv
DB_HOST_PORT=5433
DB_USER=postgres
DB_PASSWORD=replace_with_your_password
JWT_SECRET=replace_with_a_long_random_secret
```

`DB_HOST_PORT` controls the PostgreSQL port exposed on the host. It defaults to `5433` when omitted.

## 2. Configure the Local API

Create the ignored local settings file:

Set `ConnectionStrings:DefaultConnection` in `appsettings.json`. The port must match `DB_HOST_PORT` from `.env`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=PRM393_Cleaning;Username=postgres;Password=replace_with_your_password"
  }
}
```

The files are intentionally separate:

- `.env` configures Docker Compose.
- `appsettings.json` configures a locally executed API and EF commands.

## 3. Start PostgreSQL

```powershell
docker compose up -d db
```

## 4. Apply Migrations and Seed Development Data

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project DAL --startup-project CleaningService.API
```

Applying migrations in Development also invokes EF Core `UseSeeding`/`UseAsyncSeeding`. The seed operation is idempotent, so running the command again does not duplicate data.

Seeded accounts:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@cleanai.local` | `CleanAI123!` |
| Client | `client@cleanai.local` | `CleanAI123!` |
| Worker | `worker@cleanai.local` | `CleanAI123!` |

The seeder also creates two cleaning services, profiles, a client address, worker skills and availability, and AI knowledge documents. Demo data is not seeded outside Development.

## 5. Run the API

```powershell
dotnet run --project .\CleaningService.API\CleaningService.API.csproj
```

The default launch profile serves the API at:

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`

## Verify the Setup

Open Swagger and call:

```http
POST /api/Auth/login
Content-Type: application/json
```

```json
{
  "emailOrPhone": "client@cleanai.local",
  "password": "CleanAI123!"
}
```

A successful response containing access and refresh tokens confirms that the migration, seeder, database connection, and authentication flow are working.

The public service catalog can be checked with:

```http
GET /api/ServiceCatalog/categories
```

## Optional: Start Ollama

The chatbot requires Ollama:

```powershell
docker compose up -d ollama
```

The default model is `qwen2.5:1.5b`. Pull it if it is not already available:

```powershell
docker exec local_ollama ollama pull qwen2.5:1.5b
```

## Useful Commands

Build the backend:

```powershell
dotnet build .\CleaningServiceApp.sln
```

Create a migration:

```powershell
dotnet ef migrations add MigrationName --project DAL --startup-project CleaningService.API --output-dir Migrations
```

List migrations:

```powershell
dotnet ef migrations list --project DAL --startup-project CleaningService.API
```

Stop Compose services without deleting data:

```powershell
docker compose down
```

## Troubleshooting

### `MSB1009: Project file does not exist`

Run commands from `CleaningServiceApp`, or provide the full project path:

```powershell
dotnet run --project .\CleaningService.API\CleaningService.API.csproj
```

### `Format of the initialization string does not conform to specification`

Check `ConnectionStrings:DefaultConnection` in `appsettings.json`. Passwords containing semicolons must be quoted as shown above.

### Seeded login returns HTTP 401

Confirm all of the following:

- The migration command ran with `ASPNETCORE_ENVIRONMENT=Development`.
- The API and EF command use the same database and host port.
- The request field is `emailOrPhone`, not `email`.

### PostgreSQL port is already in use

Choose another host port in `.env`, update the matching port in `appsettings.json`, and recreate the database container:

```powershell
docker compose up -d --force-recreate db
```

## Security Notes

- Never commit `.env` or `appsettings.json`.
- Replace all example secrets before deploying.
- Seeded credentials are for local Development only.
- Production migrations should run through a controlled deployment process.

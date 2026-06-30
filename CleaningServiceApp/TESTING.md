# Backend testing

Automated tests never use the development `cleaning_db` database.

## Commands

```powershell
dotnet test CleaningServiceApp.sln
dotnet test tests\Cleaning.BLL.Tests\Cleaning.BLL.Tests.csproj
dotnet test tests\CleaningService.API.Tests\CleaningService.API.Tests.csproj
dotnet test CleaningServiceApp.sln --collect:"XPlat Code Coverage"
```

API integration tests require Docker. Each run starts a disposable PostgreSQL 16 container, applies every EF migration, and resets application tables between tests.

## Adding a test

- Put isolated business-rule tests in `Cleaning.BLL.Tests`.
- Put routing, authentication, serialization, migration, transaction, and database constraint tests in `CleaningService.API.Tests`.
- Write the display name in Vietnamese and prefix it with the feature test ID, for example `[UT-BE-BOOK-001-01]` or `[IT-BE-AUTH-001-01]`.
- Do not depend on test order or development seed data.
- Replace email, SMS, AI, push, and payment providers in the test factory before testing workflows that call them.

## CI/CD

Pull requests and default-branch pushes run quality checks, unit tests, integration tests, and Docker image validation independently. A default-branch push publishes `cleaning-api:sha-<commit>` and `cleaning-api:latest` only after every required check passes.

Configure these GitHub Actions secrets before enabling Docker publication:

- `DOCKERHUB_USERNAME`
- `DOCKERHUB_TOKEN`

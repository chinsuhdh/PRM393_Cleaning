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

- Put business-rule tests in `Cleaning.BLL.Tests`.
- Put routing, authentication, serialization, migration, and database constraint tests in `CleaningService.API.Tests`.
- Write the display name in Vietnamese and prefix it with the feature test ID, for example `[UT-BE-BOOK-001-01]`.
- Do not depend on test order or development seed data.
- Replace email, SMS, AI, and payment providers in the test factory before testing workflows that call them.

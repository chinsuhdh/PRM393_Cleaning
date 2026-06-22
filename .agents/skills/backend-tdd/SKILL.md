---
name: backend-tdd
description: Pragmatic test-driven development guidance for this repository's .NET backend. Use when Codex is adding or changing backend behavior, fixing bugs, designing regression coverage, creating xUnit tests, deciding unit versus API integration tests, or updating CI/test commands for CleaningServiceApp.
---

# Backend TDD

## Testing Posture

Use pragmatic TDD: write the first useful failing test before implementation when behavior is meaningful, risky, or bug-related. Do not force tests for trivial DTO/property-only changes.

Default stack:

- Use xUnit for .NET tests.
- Use Moq when mocking clarifies a dependency boundary.
- Prefer simple in-memory fakes when they make behavior easier to read than mock setups.
- Use ASP.NET Core `WebApplicationFactory` for API integration tests when endpoint behavior, routing, auth, filters, or serialization matter.

## Test Placement

- Put backend tests under `CleaningServiceApp/tests`.
- Use project names that mirror the production target, such as `Cleaning.BLL.Tests` and `CleaningService.API.Tests`.
- Test BLL services first for business behavior because they sit between controllers and persistence.
- Keep DAL tests focused on persistence mapping/query behavior; avoid testing EF Core itself.
- Add API integration tests for externally visible HTTP behavior, not for every service branch.

## TDD Loop

For each behavior change:

1. State the expected behavior and edge cases from the user request or `plan.md`.
2. Choose the narrowest useful test level:
   - BLL unit test for business rules.
   - API integration test for request/response behavior.
   - DAL/persistence test only when data access behavior is the point.
3. Add or update a failing test first when practical.
4. Implement the minimum production change.
5. Run the targeted test, then broaden to backend checks.
6. Refactor only after tests pass.

## Test Quality

- Assert observable behavior rather than private implementation details.
- Prefer clear arrange/act/assert structure.
- Name tests by behavior, not method mechanics.
- Cover authorization, validation, status transitions, exception paths, and regression cases when relevant.
- Avoid brittle interaction tests unless the interaction is the contract.
- Avoid broad snapshot-style assertions for API responses unless the response shape is the contract being protected.

## Verification

When test projects exist, run:

```powershell
dotnet test CleaningServiceApp\CleaningServiceApp.sln --configuration Release --no-build
```

If adding the first test project, also update the solution and CI so `dotnet test` becomes part of backend validation.

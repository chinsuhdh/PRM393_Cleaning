# Repository Agent Guidance

## Repo Shape

- Backend lives in `CleaningServiceApp` and follows API/BLL/DAL layering.
- Keep HTTP concerns in `CleaningService.API`, business workflows and DTOs in `BLL`, and entities/DbContext/repositories/migrations in `DAL`.
- Reusable Codex skills live in `.agents/skills`.
- Use `plan.md` at the repo root for one active implementation plan.

## Default Workflow

- Read `plan.md` before implementing plan-driven work.
- Keep changes scoped to the explicit plan and preserve existing structure.
- Use `$clean-code-architect` for architecture, file-size, constants, and abstraction decisions.
- Use `$backend-tdd` for test strategy, regression coverage, and test-first implementation.
- For review, impact analysis, or broad changes, prefer the installed `code-review-graph` CLI over MCP/hooks:
  - Run `code-review-graph update --brief --base HEAD~1` when a graph already exists.
  - Run `code-review-graph build` first if the graph is missing or stale.
  - Run `code-review-graph detect-changes --brief --base HEAD~1` for read-only impact context after the graph is current.
  - If graph commands fail, report the failure and continue with direct repo inspection.

## Verification

Run the smallest meaningful checks for the change. Backend checks normally include:

```powershell
dotnet restore CleaningServiceApp\CleaningServiceApp.sln
dotnet format CleaningServiceApp\CleaningServiceApp.sln --verify-no-changes --no-restore --verbosity minimal
.github\scripts\check-backend-architecture.ps1
.github\scripts\check-backend-file-size.ps1
dotnet build CleaningServiceApp\CleaningServiceApp.sln --configuration Release --no-restore --disable-build-servers /m:1 /p:UseSharedCompilation=false
```

Add `dotnet test` once test projects exist.

## Branches And Commits

- Do not create a new branch unless the user explicitly asks, except when the current branch is `main` or `master`.
- If the current branch is `main` or `master` and the user asks to commit/package work, create a focused task branch before committing.
- If already on a non-main task branch, keep using the current branch unless the user asks for a different branch.
- Commit only after implementation and verification are complete.
- Commit summaries should cover: summary, key changes, checks run, code-review-graph context used, and known gaps.
- Push only when the user explicitly asks.

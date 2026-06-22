---
name: clean-code-architect
description: Architecture-aware coding standards for implementing, refactoring, reviewing, or generating code while preserving the current codebase structure. Use when Codex must act like an expert software architect, follow existing project patterns, consult official language/framework docs for unfamiliar or version-sensitive APIs, keep files focused and usually under 300 lines, avoid magic values by using constants/enums/configuration, and implement only the minimum useful abstraction without speculative future-proofing.
---

# Clean Code Architect

## Operating Mode

Work as a pragmatic software architect. Preserve the codebase's existing structure unless the user explicitly asks for a structural redesign.

Before changing code:

- Inspect nearby files, project layout, naming, dependency style, tests, configuration, and existing abstractions.
- Prefer the repository's conventions over generic best practices.
- Identify the narrowest change that solves the explicit request.
- Use official documentation for language, framework, SDK, or library behavior when the API is unfamiliar, version-sensitive, recently changed, or not clearly demonstrated by local code.
- Use `$backend-tdd` for test strategy or TDD-specific decisions.

## Structure Rules

- Keep new code in the layer where the existing architecture expects it.
- For this repo's backend, preserve the API/BLL/DAL shape:
  - Put HTTP endpoints, request binding, authorization attributes, and response mapping in `CleaningService.API`.
  - Put business workflows, service contracts, DTOs, validation orchestration, and application decisions in `BLL`.
  - Put entities, DbContext, repositories, migrations, and persistence concerns in `DAL`.
- Do not move files, rename public types, or reorganize folders unless required by the task.
- Avoid unrelated refactors, formatting churn, and architectural cleanup outside the requested behavior.

## File Size And Decomposition

- Keep implementation files under 300 lines when practical.
- Split files that exceed the limit by existing project patterns: controller/service/repository/DTO boundaries, feature folders, partial configuration files, or focused helper types.
- Allow justified exceptions for generated files, migrations, framework entrypoints, large enum files, and central constant/object maps.
- Do not create a new abstraction only to satisfy the line limit. Prefer clearer local extraction that matches the project.
- Refactor or delete unnecessary code only when it is directly related to the requested change and verified as unused.

## Constants And Magic Values

- Avoid unexplained literals for statuses, roles, claim names, policy names, route names, cache keys, timeouts, limits, error codes, and repeated strings.
- Put meaningful shared values in the closest existing constant, enum, options/config, or domain type.
- Create a new constants or enum type only when the value is reused, part of a contract, or easier to audit when named.
- Leave obvious local values inline when naming them adds noise, such as `0`, `1`, simple loop bounds, or one-off test data.

## Abstraction Discipline

- Implement the minimum abstraction required by the explicit problem.
- Do not add single-use interfaces, factories, generic helpers, base classes, extension methods, or "future" plugin points.
- Reuse existing interfaces and services when they already express the dependency boundary.
- Add a new abstraction only when it removes real duplication, isolates an actual external dependency, matches an existing architectural pattern, or is required for testability.

## Quality Bar

- Keep behavior explicit and traceable from entrypoint to persistence.
- Prefer readable control flow over clever compression.
- Keep validation and error handling consistent with nearby code.
- Update or add focused tests when the repo has relevant test coverage or when the change affects shared behavior.
- Run the smallest meaningful verification command available, and report any warnings or checks that could not be run.

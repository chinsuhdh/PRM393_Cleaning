# Plan To Commit

Use this prompt when the user wants Codex to implement the active `plan.md` and package the result as a branch commit.

## Inputs

- Read `AGENTS.md`.
- Read root `plan.md`.
- Read only relevant skills from `.agents/skills`.
- Inspect the smallest set of source files needed to understand and implement the plan.

## Workflow

1. Confirm the plan's goal, acceptance criteria, and out-of-scope items from `plan.md`.
2. If the plan is ambiguous enough to risk wrong implementation, ask before editing.
3. If the user asked to package the work and the current branch is `main` or `master`, create a focused task branch before committing.
4. Use `$clean-code-architect` for structure, constants, file-size, and abstraction choices.
5. Use `$backend-tdd` for backend test strategy and regression coverage.
6. For review, impact analysis, broad refactors, or risky backend changes, use the installed code-review-graph CLI:
   - Run `code-review-graph update --brief --base HEAD~1` when `.code-review-graph/graph.db` exists.
   - Run `code-review-graph build` first if the graph is missing or stale.
   - Run `code-review-graph detect-changes --brief --base HEAD~1` after the graph is current for read-only impact context.
   - If a command fails, report it and continue with direct inspection.
7. Implement the minimum code and test changes needed for the plan.
8. Run the smallest useful verification commands from `AGENTS.md`.
9. Inspect `git diff` and review the change before committing.
10. Commit only the intended files with a concise conventional commit message.

## Final Output

Report:

- Branch name
- Commit hash
- Summary
- Key changes
- Tests/checks run
- Code-review-graph context used, if any
- Known risks or gaps

Push only when the user explicitly asks.

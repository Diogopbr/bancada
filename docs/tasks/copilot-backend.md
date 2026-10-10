# Copilot backend assignment

User explicitly approved collaboration. Implement tasks 2, 3, 4 and 5 of the approved plan in your assigned worktree only. Read the spec, plan and docs/contracts/http.md; contract takes priority for exact wire DTOs. Do not modify frontend, deployment or root scripts. You own backend/Bancada.Api and backend/Bancada.Tests; preserve readiness tests.

Work test-first per feature: run a failing test, implement, rerun suite. Use real PostgreSQL integration with isolated database/schema for each test fixture, using BANCADA_TEST_CONNECTION as admin connection; tests must not silently skip. Do not use SQLite. Capture RED/GREEN commands and test counts in docs/tasks/copilot-backend-result.md. The shared database must not be dropped or mutated by destructive reset; create a uniquely named test database and clean only that database.

Use ASP.NET Core Identity for staff. Domain/version changes and event insertion are transactional. Add migrations and deterministic fictional seed runnable in Development. Email identities atendente@bancada.example, tecnico1@bancada.example, tecnico2@bancada.example; password from BANCADA_DEMO_PASSWORD environment/configuration only. Explicit production seed switch; never default production demo password.

Ensure app startup in tests can configure a separate database; readiness tests should not require migrations or seeding to start. Defer schema migration/seeding to an explicit bootstrap command if needed. No real email services. Add authorization, antiforgery, rate limiting and customer access/decision with version consistency. Read DTO contract carefully, including money strings and public projection.

Root SDK: D:/Eu/Projetos/.tools/dotnet/dotnet.exe. Dot source D:/Eu/Projetos/1/scripts/local-env.ps1 to set SDK and local PostgreSQL connection. Never print secrets. Do not commit, push, deploy or touch existing services. Complete each task and record evidence; Codex reviews and commits. If an interface is ambiguous, record your decision with rationale in result file.

# Bancada HTTP contract

Source of truth for implementers; camelCase JSON. API base `/api`. Decimal money is serialized as string with two decimal places. All business timestamps ISO UTC. Responses never contain password, token hash or other customer's information.

## Session

GET `/csrf` => `{token:string}`; send `X-CSRF-TOKEN` with state-changing requests including login. GET `/session` => `{id:string,displayName:string,role:'receptionist'|'technician'}` or 401. POST `/login` `{email,password}` => session; POST `/logout` => 204. GET `/technicians` => array `{id,displayName}`.

## Orders

GET `/orders?search=&status=&technicianId=&page=1&pageSize=20` => `{items:OrderSummary[],total:number,page:number,pageSize:number}`. Maximum pageSize 50. Unknown filter returns 400.

OrderSummary: `{id,code,customerName,equipmentLabel,status,technicianId,technicianName,updatedAt,version}`. OrderDetail adds `{customerEmail,equipmentSerial,reportedIssue,diagnosis,currentQuote:Quote|null,history:HistoryEvent[]}`. Nullable technician is not allowed at creation. Status values received, diagnosing, awaitingApproval, approved, repairing, ready, delivered, cancelled.

POST `/orders` `{customerName,customerEmail,equipmentLabel,equipmentSerial,reportedIssue,technicianId}` => 201 OrderDetail. Names 1..120; email validated; serial 0..100; issue 1..2000. GET `/orders/{id}` => OrderDetail.

POST `/orders/{id}/diagnosis` `{expectedVersion,diagnosis}` => OrderDetail. Technician assigned to order only; starts diagnosis if received, edits diagnosis while diagnosing; diagnosis 1..4000 chars. POST `/orders/{id}/transitions` `{expectedVersion,target,reason?}` => OrderDetail. Only specified spec transitions allowed. Reason required for cancellation.

## Quotes

POST `/orders/{id}/quotes` `{expectedVersion,items:[{description,kind:'part'|'labor',quantity:integer,unitPrice:string}]}` => OrderDetail. 1..100 items; description 1..200 chars; quantity 1..999; unitPrice 0..999999.99 with max 2 decimals, invariant format. Revisions append only, server calculates total; no client total accepted as authority. Publish only while diagnosing, awaitingApproval or approved, never repairing or terminal. Diagnosis required.

Quote: `{revision:number,items:QuoteItem[],total:string,publishedAt:string,decision:'accepted'|'rejected'|null}`. HistoryEvent: `{id,action,actorName,createdAt,detail}`; detail is human-readable Portuguese, no token or password. Stable action identifiers: received, diagnosisUpdated, quotePublished, quoteAccepted, quoteRejected, repairStarted, repairCompleted, delivered, cancelled.

## Customer

POST `/orders/{id}/customer-access` `{expectedVersion}` => `{url:string,expiresAt:string}`. Receptionist creates/replaces token; 32 bytes randomness; 7 days; hash stored. DELETE same path => 204 and revokes grants/sessions for that access. Token remains in URL fragment only: `/acompanhar#token=...`.

POST `/customer/session` `{token}` => 204 HttpOnly customer cookie; csrf required. GET `/customer/order` => `{code,equipmentLabel,status,version,currentQuote,history}` excluding internal diagnosis, emails and notes. POST `/customer/decision` `{quoteRevision,decision:'accepted'|'rejected'}` => same public projection. CSRF and rate limit 20/minute/IP apply. Repeating same valid decision returns current projection with no duplicate event; opposite decision or superseded revision => 409. Session expires no later than access and is checked against revoked grant on every request.

## Errors

ProblemDetails `{type,title,status,code,detail?,errors?}`. Stable codes validation, unauthorized, forbidden, notFound, conflict, rateLimited. Invalid/expired/revoked client access returns generic 404; no token existence hints. Stale version => 409 before changing data. A missing team session => 401, insufficient role => 403. Immutable revisions and history persist across restarts.

## Local tooling

Repo root D:/Eu/Projetos/1; SDK D:/Eu/Projetos/.tools/dotnet/dotnet.exe. Dot source root scripts/local-env.ps1 to set SDK and local connection; do not print connection string. Private PostgreSQL port 55417 (existing service on 5432 preserved). Health `/health/ready` 200 iff PostgreSQL connects. Use local root settings only for local testing, never copy .local into worktrees or Git.
# Demonstração privada

Extensão da tarefa 8, disponível quando `BANCADA_DEMO_MODE=true`:

- `GET /api/demo` -> `{enabled:boolean}`.
- `POST /api/demo/start {}` -> sessão de atendente de um workspace privado. Reutiliza o visitante válido; cookie opaco HttpOnly.
- `POST /api/demo/role {role:"receptionist"|"technician1"|"technician2"}` -> sessão, revogando as anteriores do workspace.
- `POST /api/demo/reset {}` -> novo workspace e sessão de atendente; revoga dados/acessos anteriores.
- DTO de sessão acrescenta `demoRole` opcional/nulo; `role` permanece `receptionist|technician`. Esse campo recupera o técnico correto depois de recarregar.
- CSRF em todas as mutações, 20 requisições/minuto/IP para start/role/reset, até 20 ordens totais por workspace e expiração fixa em 24h.
- Cinco exemplos fictícios fazem parte da quota: entrada, diagnóstico, orçamento pendente, revisão superada e entrega concluída.
- O modo normal preserva a oficina única. Contas globais não entram na demo. O namespace interno não aparece nos DTOs de ordem.

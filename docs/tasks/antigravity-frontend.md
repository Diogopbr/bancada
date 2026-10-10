# Antigravity frontend assignment

Implement the approved Bancada interface in the assigned worktree, with Angular 21 standalone components, Signals for local state and Reactive Forms. Read docs/contracts/http.md, the spec and Task 6 in the plan first. You own frontend/ only plus docs/tasks/antigravity-frontend-result.md. Do not edit backend or contracts. No commit, push or deploy.

## Product and visual direction

Bancada is a small repair workshop's daily workspace. This is a working product, not a marketing landing page. Portuguese Brazilian copy. Warm paper (#f7f5ee), ink (#242924), muted moss (#486347), warm orange accent (#b84b27) sparingly. Use typography with a practical character (self-host an openly licensed font if justified; a system stack is acceptable while offline). Sharp typographic hierarchy, measured spacing, mostly flat surfaces, clear table rows and timeline, no generic purple gradients, shiny glass, giant dashboard cards or invented metrics. Real status labels + color cues. Use one coherent component vocabulary.

Desktop layout: restrained side navigation with Bancada wordmark, active 'Ordens de serviço', session name/role and logout. Main workspace: compact heading, 'Nova ordem' action for receptionist, search and status/technician filters, useful order rows showing code, equipment/customer, technician, status, update. Main list must support keyboard and mobile transforms without hiding actions. Empty list, no matches and server error each have appropriate action and copy.

Order detail: header with code/status; equipment and reported issue; diagnosis and technician; current budget revision with line items and total; contextual next action; narrow chronological history. Technician edits diagnosis and publishes quotes. Receptionist creates order, generates customer link and confirms delivery/cancellation. Full form with clear labels, inline validation, pending feedback and preserved values on error. Quote editing creates a new revision; clearly explain that the client must approve again.

Customer route: comfortable mobile sheet with workshop identity, equipment/order/status, line items/total/revision and explicit Approve/Reject confirmation. Do not show internal notes or staff controls. On token exchange remove URL fragment before loading data. Expired access gives understandable recovery guidance. Public projection fields must match HTTP contract.

## Implementation requirements

Routes /entrar, /ordens, /ordens/nova, /ordens/:id, /acompanhar. API is same-origin /api, proxy provided. Core typed API client; include CSRF token for writes and credentials. Load GET /csrf before login/token exchange. Runtime permission is server-owned; controls depend on role/order state only for UX. Wire money string format directly; display BRL. Do not invent mock success or use localStorage as real persistence. HTTP 409 explains stale data and reloads on user action. 401 returns to login with meaningful message. Escape human text via standard Angular interpolation.

Current contract has no dedicated 'start diagnosis' endpoint: posting diagnosis starts it from received; transition actions for repairing, ready, delivered and cancelled. For customer link offer copy/open with actual returned url only.

Use test-first for list states named in Task 6. Replace generated Angular welcome page and adjust app.spec tests to actual product behavior. Record RED then GREEN evidence in result file. npm install as needed (lockfile preserved), npm run build, npm test -- --watch=false. No install of new design systems without a need. Do not add fake chart metrics or unrelated pages. Deliver complete functional screens against contract, no placeholder tabs.

Read C:/Users/diogo/.codex/skills/design-taste-frontend/SKILL.md for design judgment, applying product spec when generic landing-page guidance differs. Reduced motion, visible focus, semantic controls, adequate contrast and 390px viewport required. At finish, summarize exact changed files, commands/results and any blocker in docs/tasks/antigravity-frontend-result.md. Backend may still be implementing; do not declare live integration verified until Codex checks it.

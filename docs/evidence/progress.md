# Bancada — execução

Especificação e plano aprovados em 10/10/2026. Implementação iniciada.

## Ambiente

- Node 24.8.0, npm 11.6.0.
- PostgreSQL 17.10 instalado; serviço existente preservado.
- SDK .NET 10.0.401 instalado em diretório local de ferramentas.
- PostgreSQL privado na porta 55417 pronto, com senha aleatória fora do Git.
- Branch local codex/bancada; commits fe5f17d e 9661c01, ainda sem push ou deploy.

## Preferência de publicação

Vercel para interface, conforme instrução do usuário. Avaliar Container Runtime beta para manter backend .NET no mesmo domínio. Referência: https://vercel.com/docs/functions/runtimes/container

## Agentes externos

Copilot: conexão comprovada pelo relatório docs/tasks/copilot-connection-check.md e confirmação visual do usuário. Backend enviado para .worktrees/copilot, branch agent/copilot-backend.

Antigravity: comando chat do IDE abriu janela sem prompt (confirmado pelo usuário). CLI oficial 1.3.3 instalado em diretório local; tentativa headless em andamento. Ainda sem relatório de conclusão.

## Verificações iniciais

- Readiness: RED 2 testes falharam pela rota ausente; GREEN 2 passaram com PostgreSQL real, zero warnings de build.
- Build Angular passou. Testes Angular gerados: 2 passaram; isto é evidência de scaffolding, não de cobertura do produto.
- HTTP real: API readiness 200 e servidor Angular 200.
- verify.ps1 passou na base atual; fluxos de negócio e E2E ainda não implementados.
- Dependência transitiva Microsoft.OpenApi 2.0.0 teve alerta de vulnerabilidade; referência explícita 2.12.2 removeu o alerta na execução seguinte.

# Copilot: resultado da demo isolada e hosting

Data: 2026-10-10. Worktree existente: `D:\Eu\Projetos\1\.worktrees\copilot`.
Branch: `agent/copilot-backend`. Base preservada: `e253c1f`.
Escopo: backend, testes e este relatorio. Sem commit, push, deploy ou alteracao de frontend.

## Implementacao

- `BANCADA_DEMO_MODE=true` habilita a demo; ausente/false preserva a oficina unica.
- [DemoSessionEndpoints.cs](../../backend/Bancada.Api/Features/Access/DemoSessionEndpoints.cs):
  - `GET /api/demo` retorna somente `{enabled}`.
  - `POST /api/demo/start {}` cria/reutiliza workspace e retorna `{id, displayName, role}`.
  - `POST /api/demo/role {role}` aceita `receptionist`, `technician1`, `technician2`; os dois tecnicos retornam papel wire `technician`.
  - `POST /api/demo/reset {}` revoga o workspace anterior e retorna um novo atendente.
  - Mutacoes usam CSRF existente e limite de 20 requisicoes/minuto/IP na politica `demo`.
- Cookie `bancada.demo`: token aleatorio opaco de 256 bits, somente hash no PostgreSQL, HttpOnly, SameSite Strict, Secure obrigatorio em producao. Permanece depois do logout; nao exige senha compartilhada.
- Cada visitante recebe tres usuarios privados sem senha e tres ordens ficticias: recebida, em diagnostico e aguardando aprovacao com orcamento.
- Inicio repetido e concorrente com o mesmo cookie reutiliza o workspace. Troca de papel revoga as sessoes anteriores daquele workspace, inclusive cookies reapresentados.
- Expiracao fixa em 24 horas desde a criacao, sem renovacao por inicio, troca de papel ou atividade. Sessao de equipe continua limitada a oito horas; cookie de visitante permite nova entrada no workspace ainda valido.
- [BancadaDb.cs](../../backend/Bancada.Api/Infrastructure/BancadaDb.cs) aplica filtro de workspace e validade nas ordens. Equipe, filtro de tecnico e atribuicao verificam o mesmo escopo. IDs estrangeiros nao permitem leitura, diagnostico, transicao, orcamento, emissao ou revogacao de acesso.
- [DemoResetService.cs](../../backend/Bancada.Api/Infrastructure/DemoResetService.cs) usa locks PostgreSQL no visitante/workspace. Criacao da vigesima ordem e contagem ocorrem na mesma transacao com lock do workspace: tres seeds + dezessete criacoes, demais retornam 409.
- Mutacoes de ordens e cliente usam ordem de locks workspace -> ordem. Reset concorre com essas operacoes sem permitir escrita no workspace ja revogado.
- Cliente pode trocar um grant valido por sessao em outro navegador sem cookie de visitante. Navegador ja associado a outro workspace nao troca esse grant. Expiracao/reset invalida grant bruto e sessao de cliente; validade e reavaliada a cada leitura/decisao, independentemente da limpeza. Grant da demo expira junto com o workspace.
- Login comum e cookies de contas globais sao recusados no modo demo. Usuarios/grants da demo tambem nao passam a operar no modo normal.
- [DemoCleanupService.cs](../../backend/Bancada.Api/Infrastructure/DemoCleanupService.cs): timer de cinco minutos e verificacao oportunista em requisicoes `/api` apos despertar. Cada lote remove no maximo dez workspaces expirados/revogados e seus usuarios, sessoes, grants, ordens, historicos e orcamentos; preserva dados globais e workspaces validos. Locks com `SKIP LOCKED` respeitam visitante antes de workspace. Nao executa migrations.
- Provisionamento concorrente em banco migrado sem seed global foi corrigido com advisory lock transacional exclusivamente para a criacao inicial dos papeis Identity.

## Hosting

- [Program.cs](../../backend/Bancada.Api/Program.cs) serve arquivos precompilados em `wwwroot`.
- [ProductionHosting.cs](../../backend/Bancada.Api/Infrastructure/ProductionHosting.cs) fornece fallback para `index.html` somente em rotas SPA GET/HEAD sem extensao. `/api`, `/health`, assets ausentes e falhas de autenticacao/readiness nunca recebem esse fallback HTML.
- Producao usa redirecionamento HTTPS para porta externa 443, HSTS e cookies Secure. Forwarding e processado antes de autenticacao, CSRF e rate limiting.
- Key ring oficial `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` no PostgreSQL, `ApplicationName = Bancada`. Hosts compartilham chaves e revogacao persistida.
- Migration gerada pelo EF CLI: [20261010135610_DemoAndDataProtection.cs](../../backend/Bancada.Api/Infrastructure/Migrations/20261010135610_DemoAndDataProtection.cs), designer e snapshot. O CLI confirmou ausencia de mudancas pendentes de modelo.
- Pacote novo de DataProtection e dependencias EF/Identity diretamente relacionadas usam `10.0.12`. A tentativa inicial com `10.0.6` gerou alertas NuGet de DataProtection/Xml; restore/build final nao gerou esses alertas.
- [Dockerfile](../../backend/Dockerfile) publica somente o backend e roda como usuario `app`, SDK `10.0.401`, runtime `10.0.12`. Contexto de build: pasta backend, excluindo bin/obj/resultados pelo [.dockerignore](../../backend/.dockerignore).

### Configuracao externa e integracao Vercel

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Bancada=<conexao PostgreSQL externa>
BANCADA_DEMO_MODE=true
AllowedHosts=<dominio publico aprovado>
Hosting__TrustedProxies__0=<IP exato do proxy imediato controlado pelo hosting>
# Alternativa somente para redes de ingress controladas:
Hosting__TrustedNetworks__0=<CIDR restrito>
```

Nao confiar indiscriminadamente em `X-Forwarded-*` recebido da Internet. Sem proxies/redes explicitamente configurados, forwarding fica desabilitado. Valores malformados falham explicitamente.
O ingress/adapter da Vercel deve sobrescrever os headers de encaminhamento e enviar `X-Forwarded-For` e `X-Forwarded-Proto` coerentes, com um salto confiavel; IP/rede configurado deve corresponder ao peer realmente visto pelo ASP.NET. Nao encaminhamos host arbitrario.
Nenhum endereco real de ingress foi presumido nem nenhuma credencial foi alterada.

Executar `dotnet Bancada.Api.dll --bootstrap` como comando separado antes de iniciar hosts em banco novo/atualizacao. Nao usar `--seed` para a demo publica: nao precisa de contas globais nem senha publica. Startup comum nao migra o banco.
Aplicar migrations por conexao direta quando o provedor oferecer pooling transacional.
O integrador do frontend deve colocar o conteudo do build Angular em `backend\Bancada.Api\wwwroot` antes de publicar a API; nao foi produzido/substituido frontend nesta tarefa.

## RED/GREEN observado

Todos os comandos .NET carregaram primeiro `D:\Eu\Projetos\1\scripts\local-env.ps1`.
SDK observado: `10.0.401`; runtime instalado: `10.0.12`; EF CLI instalado fora da worktree em `D:\Eu\Projetos\.tools\copilot-ef`, versao `10.0.12`.
O runner de testes do editor nao encontrou testes nesses arquivos; a execucao efetiva foi pelo runner xUnit de `dotnet test`.
PostgreSQL real via `BANCADA_TEST_CONNECTION`, sem mocks, SQLite ou skips. Fixtures criam bancos `bancada_test_<UUID>` e removem somente esses bancos de teste. Banco compartilhado nao foi resetado, migrado ou apagado.

| Etapa | Total | Passaram | Falharam | Ignorados |
|---|---:|---:|---:|---:|
| RED inicial: demo/hosting ausentes | 11 | 1 | 10 | 0 |
| GREEN inicial | 11 | 11 | 0 | 0 |
| RED adicional: oito primeiros visitantes sem seed | 1 | 0 | 1 | 0 |
| GREEN final direcionado | 17 | 17 | 0 | 0 |
| GREEN final completo | 61 | 61 | 0 | 0 |

RED adicional confirmou PostgreSQL `23505`, constraint `RoleNameIndex`, nao uma suposicao de concorrencia.
Os 44 testes originais foram preservados e passaram junto aos 17 novos casos. Suite completa final: 27 segundos; direcionada final: 21 segundos.

Comandos finais, a partir da worktree:

```powershell
. D:\Eu\Projetos\1\scripts\local-env.ps1
dotnet test backend\Bancada.Tests --filter 'FullyQualifiedName~DemoIsolationTests|FullyQualifiedName~HostingTests'
dotnet test backend\Bancada.slnx --no-build
& D:\Eu\Projetos\.tools\copilot-ef\dotnet-ef.exe migrations has-pending-model-changes --project backend\Bancada.Api
dotnet publish backend\Bancada.Api -c Release --output backend\Bancada.Api\bin\demo-publish
```

Publicacao Release local concluiu com sucesso, sem deploy. TRXs RED/GREEN preservados no armazenamento local desta sessao, fora da worktree.

### Evidencia dos testes

- [DemoIsolationTests.cs](../../backend/Bancada.Tests/DemoIsolationTests.cs): isolamento por visitante em todos os endpoints de ordem, tecnico estrangeiro, cliente separado por posse de grant, outro workspace preservado pelo reset, inicio idempotente/concorrente, papeis opostos, replay de cookie, logout/troca, vinte ordens concorrentes incluindo seed, validade fixa, expiracao/reset de equipe/cliente/token, CSRF/rate limit, limpeza limitada, cookie opaco Secure e provisionamento concorrente sem seed.
- [HostingTests.cs](../../backend/Bancada.Tests/HostingTests.cs): arquivos/fallback, API/health/assets sem HTML indevido, banco indisponivel 503, forwarding confiavel versus spoofing, key ring PostgreSQL entre hosts e revogacao apos reinicio.
- Reinicio tambem verificado em dois processos Kestrel reais, mesma conexao PostgreSQL: equipe e cliente permanecem autenticados; cookie de visitante troca papel apos reinicio; reset invalida cliente e cookie antigo reapresentado. Ambos os processos foram encerrados pelo teste.
- Bootstrap explicito e ausencia de schema na primeira inicializacao comum continuam verificados pelos testes existentes.

## Limites e premissas

- Nenhum deploy, push, commit ou modificacao de frontend. Nenhuma URL publica foi anunciada.
- Hosting de assets foi exercitado com arquivos estaticos de teste, nao com um build Angular produzido pelo frontend de outro agente.
- Docker nao esta disponivel neste ambiente; Dockerfile nao foi construido/executado. O backend foi compilado/publicado e o processo Kestrel foi executado.
- Adapter/runtime e ingress reais da Vercel nao foram implantados/certificados nesta tarefa. Configuracao de proxy foi testada localmente com peer confiavel explicito e header forjado recusado.
- Key ring exige controle rigoroso do usuario PostgreSQL, protecao do armazenamento e backup: nao foi configurado certificado/provedor adicional de criptografia XML das chaves em repouso.
- Sem cookie de visitante, requisicoes anonimas simultaneas nao possuem identificador comum e criam visitantes independentes. Com cookie valido, inicio concorrente e limitado a um workspace.
- Quota e por workspace atual, nao por IP ou pessoa; reset inicia novo workspace e invalida o anterior. Provisao/reset estao sujeitos ao rate limit.
- Limpeza depende de processo ativo ou nova requisicao API; expiracao e revogacao continuam eficazes mesmo antes de remover fisicamente os dados. Workspaces bloqueados podem aguardar lote posterior.

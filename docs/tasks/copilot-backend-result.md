# Bancada backend - resultado verificavel

Concluido em 2026-10-10, 10:47 -03:00, nesta janela existente.
Worktree: `D:\Eu\Projetos\1\.worktrees\copilot`.
Branch verificada: `agent/copilot-backend`.
Tarefas 2, 3, 4 e 5 implementadas. Sem commit, push, deploy ou alteracao de servicos existentes.
Somente backend e este relatorio foram alterados. Frontend, scripts da raiz e deployment foram preservados.

## Entrega

- **2 - Acesso da equipe:** ASP.NET Core Identity/PostgreSQL; atendente e dois tecnicos; login generico, lockout, sessao e logout; cookies HttpOnly/SameSite Strict e Secure em Production; antiforgery inclusive no login. Sessoes de equipe persistidas permitem revogar imediatamente o cookie no logout, inclusive contra replay.
- **3 - Ordens:** criacao exclusiva do atendente, atribuicao obrigatoria, detalhe, busca/filtros/paginacao, diagnostico pelo tecnico atribuido, matriz de transicoes, cancelamento com motivo, entrega pelo atendente e terminais protegidos. Eventos e versao gravados atomicamente.
- **4 - Orcamentos:** revisoes append-only, itens ordenados, precos decimais invariant com ate duas casas, quantidade/valor/quantidade de itens limitados, total calculado pelo servidor e strings monetarias com duas casas na resposta. Nova revisao preserva a anterior e exige nova aprovacao. Publicacao durante/depois do reparo recusada.
- **5 - Cliente:** token de 32 bytes aleatorios em fragmento; somente hash SHA-256 no banco; validade de sete dias; substituicao/revogacao invalida todas as sessoes do acesso anterior. Cookie dedicado, sem privilegios de equipe; validade do grant conferida em toda leitura/decisao. Decisao repetida e idempotente; oposta ou de revisao superada resulta em 409. Projecao publica sem emails, diagnostico, serial, problema relatado, motivo interno de cancelamento ou token. Limite compartilhado entre endpoints de cliente de 20 requisicoes/minuto/IP; 21a resulta em ProblemDetails 429.
- **Persistencia:** cinco migrations EF Core: IdentityInitial, Orders, Quotes, CustomerAccess e StaffSessions. Snapshot sem alteracoes pendentes.
- **Seed:** tres usuarios com os emails contratados e tres ordens ficticias deterministicas (received, diagnosing, awaitingApproval). Idempotente, nao reseta dados nem altera ordens existentes. Senha somente de `BANCADA_DEMO_PASSWORD` externo. Fora de Development exige `BANCADA_ALLOW_PRODUCTION_SEED=true`, alem da senha externa.

Implementacao em [Features](../../backend/Bancada.Api/Features), [Infrastructure](../../backend/Bancada.Api/Infrastructure) e [testes](../../backend/Bancada.Tests). Contrato aplicado: [HTTP](../contracts/http.md).

## Comandos e evidencias RED/GREEN

Todos os comandos dotnet foram executados na worktree apos:

```powershell
. 'D:\Eu\Projetos\1\scripts\local-env.ps1'
Set-Location 'D:\Eu\Projetos\1\.worktrees\copilot'
```

SDK utilizado: `D:\Eu\Projetos\.tools\dotnet\dotnet.exe`.
Conexao obtida de `BANCADA_TEST_CONNECTION`, sem imprimir credenciais.
PostgreSQL real via Npgsql, sem SQLite e sem skips.

| Etapa | Comando | Resultado observado |
| --- | --- | --- |
| 2 RED | `dotnet test backend\Bancada.Tests --filter StaffAccess --logger "console;verbosity=minimal"` | 3 falhas, 0 aprovados, 0 ignorados; endpoints ausentes (404) |
| 2 GREEN | `dotnet test backend\Bancada.Tests --filter "StaffAccess\|Health" --logger "console;verbosity=minimal"` | 5 aprovados, 0 falhas, 0 ignorados |
| 3 RED | `dotnet test backend\Bancada.Tests --filter Order --logger "console;verbosity=minimal"` | 4 falhas, 0 aprovados, 0 ignorados; criacao ausente (404) |
| 3 GREEN | `dotnet test backend\Bancada.Tests --filter Order --logger "console;verbosity=normal"` | 4 aprovados; exit code 0, incluindo cleanup |
| 4 RED | `dotnet test backend\Bancada.Tests --filter Quote --logger "console;verbosity=minimal"` | 9 falhas, 0 aprovados, 0 ignorados; publicacao ausente (404) |
| 4 GREEN/regressao | `dotnet test backend\Bancada.Tests --filter "Quote\|Order\|StaffAccess\|Health" --logger "console;verbosity=minimal"` | 18 aprovados, 0 falhas, 0 ignorados |
| 5 RED | `dotnet test backend\Bancada.Tests --filter Customer --logger "console;verbosity=minimal"` | 8 falhas, 0 aprovados, 0 ignorados; acesso/decisao/rate limit ausentes |
| 5 GREEN ampliado | `dotnet test backend\Bancada.Tests --filter Customer --logger "console;verbosity=normal"` | 10 aprovados; exit code 0 |
| Regressao ampliada | Mesmo comando final com logger TRX, antes do teste adicional do bootstrap executavel | 43 aprovados, 0 falhas, 0 ignorados; exit code 0 |
| Final | comando abaixo, com TRX persistido | **44 executados, 44 aprovados, 0 falhas, 0 ignorados, 0 erros; exit code 0** |
| Build final | `dotnet build backend\Bancada.slnx --no-restore -warnaserror --verbosity minimal` | **0 avisos, 0 erros; exit code 0** |
| Migrations | `dotnet-ef migrations has-pending-model-changes --no-build --project backend\Bancada.Api` | `No changes have been made to the model since the last migration.`; exit code 0 |
| Higiene | `git diff --check` e `git status --short` | Sem erros de whitespace; somente caminhos autorizados |

Os `\|` nas tabelas escapam o separador Markdown: o filtro real usa `|`, sem a barra invertida.

Execucao final persistida:

```powershell
dotnet test backend\Bancada.Tests `
  --logger 'console;verbosity=minimal' `
  --logger 'trx;LogFileName=bancada-final.trx' `
  --results-directory 'C:\Users\diogo\.copilot\session-state\fd3d5f85-6cb5-4d56-bd8e-47d9ec10ff62\files'
```

TRX: `C:\Users\diogo\.copilot\session-state\fd3d5f85-6cb5-4d56-bd8e-47d9ec10ff62\files\bancada-final.trx`.
SHA-256: `804924D64E1450DFB98DF5CB18A449F16AEB41092E4C752679A7300D42B0C303`.
Counters do XML conferidos: total/executed/passed=44; failed/error/timeout/aborted/notExecuted=0.

O EF CLI 10.0.6 estava ausente e foi instalado apenas no diretorio de ferramentas desta sessao, sem manifesto ou scripts na raiz. As migrations foram geradas pelo CLI e aplicadas pelos testes, nao por EnsureCreated.

## O que os testes verificam

- Cada fixture HTTP cria um banco `bancada_test_<GUID>` exclusivo a partir da conexao administrativa. Primeiro inicia a API com override de conexao, confirma readiness 200 e ausencia de tabelas Identity; depois executa migrations/seed explicitamente.
- Cleanup fecha o host, usa conexoes sem pooling, remove somente o banco exato da fixture e confirma sua ausencia em `pg_database`. Nenhum DROP/reset do banco compartilhado. Os testes de readiness originais foram preservados; no banco compartilhado fazem somente `SELECT 1`.
- Duas atualizacoes de diagnostico com a mesma versao: exatamente um 200, um 409, uma nova versao e um novo evento.
- Duas decisoes simultaneas opostas: exatamente um 200, um 409 e um unico evento de decisao.
- Repeticao da decisao nao altera JSON, versao nem historico; nova revisao preserva o preco/decisao da anterior e invalida a aprovacao corrente.
- Fluxo HTTP completo: recebimento, diagnostico, orcamento, acesso, aprovacao, inicio/conclusao do reparo e entrega.
- Matriz de autorizacao/transicoes, recusa de cancelamento durante reparo e alteracoes terminais.
- `3 x 0.10 + 19.99 = "20.29"`; total fornecido pelo cliente ignorado; negativos, tres casas, overflow, virgula, expoente, quantidade 0/1000, tipos JSON invalidos e 101 itens recusados. Teto de 100 itens x 999 x 999999.99 persiste como `"99899999001.00"`, sem overflow.
- Cookies de producao seguros; CSRF ausente recusado em login/escritas de cliente; logout rejeita replay do cookie anterior.
- Invalidos, expirados, revogados e substituidos retornam 404 generico, sem detalhe identificador. Comparacao ignora apenas o traceId por requisicao.
- Cliente nao ganha acesso de equipe nem seleciona outra ordem; nova instancia de host recupera o mesmo orcamento/historico do PostgreSQL.
- Hash persistido, validade do cookie limitada ao grant e logs capturados sem token bruto, senha ficticia de teste ou cookie.
- Bootstrap executado como processo separado com `--bootstrap --seed`, banco isolado e senha externa: termina com exit code 0 sem iniciar servidor. Seed repetido nao duplica usuarios/ordens.

## Decisoes e correcoes registradas

- **Concorrencia:** token EF de versao mais lock PostgreSQL `FOR UPDATE` por ordem dentro de transacao. Diagnostico, publicacao, decisao e acesso/revogacao compartilham esse lock; validade do grant e reavaliada depois do lock, evitando uma decisao com acesso ja revogado.
- **Versao de acesso:** criacao/substituicao e revogacao efetiva incrementam a versao da ordem, sem evento de negocio adicional; o contrato nao define action de acesso. DELETE ja revogado e idempotente.
- **Projecao:** diagnostico gera texto generico no historico, nao copia nota interna; cancelamento tem motivo somente na projecao da equipe.
- **Precisao temporal:** timestamps de negocio normalizados para microssegundos, a precisao persistida pelo PostgreSQL, evitando diferenca entre a primeira resposta e a resposta idempotente apos reload.
- **Bootstrap:** startup normal nunca migra/semeia automaticamente. Para uso manual, selecionar explicitamente o banco de destino e fornecer a senha por ambiente antes de `dotnet run --project backend\Bancada.Api -- --bootstrap --seed`. Sem `--seed`, somente migrations. Nao executar esse comando contra o banco compartilhado inadvertidamente.
- **Falhas intermediarias corrigidas:** EF interpretava eventos com UUID preatribuido como update; insercao agora e marcada explicitamente. Duas execucoes intermediarias tiveram erro Npgsql no teardown; pooling foi desativado e DROP FORCE removido. As duas execucoes finais (43 e 44 testes) passaram tambem no cleanup. Verificacao inicial de schema mudou de leitura de `regclass` para booleano, tipo suportado pelo Npgsql. Tipos JSON invalidos agora geram 400 estruturado em Development, nao 500.

## Bloqueios e limites de escopo

**Nenhum bloqueio pendente para o backend das tarefas 2 a 5.**
Remover o fragmento com `history.replaceState` depois da troca por sessao pertence ao frontend da tarefa 6; nao foi implementado nem alegado como testado nesta entrega, pois frontend esta fora da permissao de edicao. O backend fornece o link em fragmento e a troca por POST conforme contrato.
Nao foram feitos testes de navegador, publicacao, deployment, reset de demo ou envio de email.
Integracao/revisao/commit continuam sob responsabilidade do Codex.

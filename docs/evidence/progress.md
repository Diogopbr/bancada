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

Antigravity: comando chat do IDE abriu janela sem prompt (confirmado pelo usuário). CLI oficial 1.3.3 autenticado e utilizado para implementar o frontend; relatório em docs/tasks/antigravity-frontend-result.md. Copilot foi mantido na janela existente do VS Code após a correção do envio inicial.

## Verificações iniciais

- Readiness: RED 2 testes falharam pela rota ausente; GREEN 2 passaram com PostgreSQL real, zero warnings de build.
- Build Angular passou. Testes Angular gerados: 2 passaram; isto é evidência de scaffolding, não de cobertura do produto.
- HTTP real: API readiness 200 e servidor Angular 200.
- verify.ps1 passou na base atual; fluxos de negócio e E2E ainda não implementados.
- Dependência transitiva Microsoft.OpenApi 2.0.0 teve alerta de vulnerabilidade; referência explícita 2.12.2 removeu o alerta na execução seguinte.

## Produto integrado — 10/10/2026

Codex repetiu os 44 testes do Copilot com PostgreSQL real, zero falhas/ignorados. Backend integrado em e8671fc. Frontend do Antigravity passou em build e 16 testes independentes e foi integrado em a8598b2.

O navegador revelou falhas de redirecionamento sem sessão, CSRF após login sem recarregar, contraste e quebra de códigos no celular. Foram corrigidas. Três regressões de precisão falharam antes da correção e passaram depois: quantidade fracionada, preço com três casas e expoente são recusados sem alteração silenciosa.

scripts/verify.ps1: exit code 0, backend build sem avisos/erros, 44 testes backend, 19 frontend e 5 Playwright aprovados. Percurso: abertura, diagnóstico, orçamento, link, aprovação, reparo e entrega. Revisão antiga recebe conflito e a atual pode ser aprovada. Teste adicional de duas abas mostrou 409 na segunda gravação e recuperação da avaliação persistida ao recarregar os dados.

axe passou no detalhe da ordem e na projeção do cliente. Layout móvel verificado a 390×844, sem overflow horizontal. Screenshots em docs/screenshots capturados da aplicação real com dados fictícios. Não houve ensaio de carga.

## Hospedagem — ainda sem deploy

Vercel autenticada, projeto bancada criado e Neon Free provisionado em gru1 após aceite dos termos pelo usuário. ConnectionStrings__Bancada foi cadastrada como segredo por stdin, com TLS VerifyFull. Valores não foram registrados no Git. Modo demo e porta 8080 configurados. Isolamento da demo e funcionamento hospedado seguem pendentes de integração/verificação.

Ainda não houve push, criação do repositório público, execução de CI remoto ou publicação no LinkedIn.

## Demo privada e revisão independente

Verificação local completa de 10/10/2026: scripts/verify.ps1 terminou com código 0, 65 testes PostgreSQL/backend, 36 testes frontend, cinco cenários de navegador normais e um cenário da demo no artefato compilado de Angular e .NET. Build backend sem avisos/erros. O teste da demo usa dois visitantes, consulta estrangeira retornando 404, cinco exemplos, troca de perfil persistida após recarregar, aprovação do cliente em outra aba, reset com teclado, invalidação do acesso antigo e preservação do outro visitante.

Revisão independente identificou quatro regressões, corrigidas e reavaliadas: versão local após revogar link; decisão sobre acesso expirado; respostas de busca fora de ordem; drenagem de workspaces vencidos. Três testes frontend falharam antes das correções e passaram depois. O teste PostgreSQL confirmou que um host remove os lotes restantes; transações mantêm limite de dez workspaces, com continuação após um segundo enquanto houver lote cheio.

A tentativa de executar comandos de teste pelo Antigravity CLI foi negada por sua política de comandos. Codex executou build e testes independentemente; esse limite está registrado em docs/tasks/antigravity-demo-result.md. A implementação externa foi integrada somente após essa validação.

## Publicação verificada — 10/10/2026

- Repositório público: https://github.com/Diogopbr/bancada.
- Demonstração pública: https://bancada-seven.vercel.app.
- CI: https://github.com/Diogopbr/bancada/actions/runs/38061163453, status completed/success para 3e2e1b4.
- Vercel: dpl_HbxTH9dxAxuPAcANmrj1WVgqXViU, READY, região gru1 confirmada pela API. Neon Free em gru1, migrations explícitas aplicadas e readiness 200 com PostgreSQL.
- Playwright hospedado passou com dois visitantes: criação persistida, dados estrangeiros 404, cookies staff/demo Secure/HttpOnly/SameSite Strict, perfil após reload, cliente em outra aba, aprovação, reparo, entrega, estado do cliente atualizado, reset com teclado e invalidação do acesso antigo. axe não encontrou violações das regras executadas na fila. O teste não substitui uma auditoria completa de acessibilidade.
- A primeira publicação em iad1 mostrou latência repetida de aproximadamente 117 ms por consulta ao banco em gru1. Aplicação movida para gru1; o percurso ampliado terminou em 17,1 s. Trata-se de observação de uma execução, não de benchmark ou ensaio de carga.

Publicação no LinkedIn ainda não executada. Rascunho preparado em docs/launch/linkedin.md para uso quando o autor decidir o calendário da coleção.

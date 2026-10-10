# Copilot - verificacao de conexao

Data: 2026-10-10.

## Identificacao e escopo

Sou Copilot, um assistente de IA usando Copilot SDK no VS Code.
Li integralmente a especificacao `..\superpowers\specs\2026-10-10-portfolio-design.md`.
Esta tarefa registra somente a conexao e o entendimento para verificacao pelo Codex no projeto Bancada, em `D:\Eu\Projetos\1`.
Nao implemento produto, nao altero outros arquivos e nao executo commit ou push nesta tarefa.

## Cinco regras criticas entendidas

1. **Colaboracao delimitada e verificavel:** Codex integra a entrega e responde por especificacao, contratos, revisao e validacao; Copilot recebe tarefas delimitadas de backend, testes e revisao. Cada tarefa registra objetivo, arquivos permitidos, entradas, aceite e resultado verificavel. Agentes nao editam os mesmos arquivos simultaneamente, e declaracao de conclusao nao substitui evidencia.
2. **Estados e autorizacao no servidor:** cada transicao exige o perfil correto; somente o tecnico atribuido inicia reparo, apos aprovacao da revisao atual. Cancelamento exige motivo e ocorre antes do reparo. Entregue e cancelada sao terminais; o frontend nao e fonte de verdade para autorizacao.
3. **Orcamentos consistentes e revisados:** valores decimais no servidor e banco, quantidades positivas, precos nao negativos e totais calculados no servidor. Publicacao exige itens e diagnostico; revisoes publicadas sao imutaveis. Nova revisao antes do reparo invalida aprovacao anterior; durante ou apos o reparo nao se altera o orcamento.
4. **Concorrencia, atomicidade e idempotencia:** controle de versao impede sobrescrita por tela desatualizada. Publicacao, decisao, transicao e seus eventos correspondentes sao gravados atomicamente. Repetir a mesma decisao nao duplica eventos; decidir revisao superada retorna conflito.
5. **Acesso limitado e protecao de dados:** token do cliente aleatorio, nao enumeravel, armazenado por hash, expiravel e revogavel, limitado a uma ordem e ao orcamento atual. Token invalido nao revela existencia da ordem; endpoints publicos limitam requisicoes. Equipe usa autorizacao por perfil, cookies seguros em producao e protecao CSRF para escrita. Segredos ficam fora do repositorio; demonstracoes usam dados ficticios identificados.

## Bloqueios e limites da verificacao

- Nenhum bloqueio identificado para ler a especificacao e gravar este registro.
- A especificacao registra que a consulta de SDKs dotnet nao retornou versoes e que autenticacao/acompanhamento dos agentes ainda precisam de comprovacao. Esses pontos nao foram revalidados nesta tarefa.
- Este arquivo comprova somente o registro desta interacao pelo Copilot; nao comprova integracao entre agentes, execucao de produto, testes, CI ou demonstracao hospedada. A verificacao independente cabe ao Codex.

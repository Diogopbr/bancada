# Coleção de portfólio — desenho aprovado

Data: 10/10/2026. Autor: Diogo Pereira. Estado: conceitos e especificação aprovados pelo usuário; plano de implementação em preparação.

## Objetivo

Construir quatro produtos independentes que ofereçam evidência verificável de desenvolvimento full stack. Cada repositório deve permitir experimentar um fluxo completo, examinar regras e testes, executar o sistema localmente e compreender as decisões técnicas. O autor deve conseguir explicar essas decisões em entrevista. Não atribuir usuários, receita, experiência comercial ou resultados fictícios aos projetos.

## Coleção e sequência

1. Pasta `1`: Bancada, operação de assistência técnica. Angular, ASP.NET Core/C#, PostgreSQL.
2. Pasta `2`: Lastro, conciliação entre pedidos e repasses. Vue, Laravel/PHP, PostgreSQL.
3. Pasta `3`: Janela, reserva de espaços e equipamentos. React, Node.js/TypeScript, PostgreSQL.
4. Pasta `4`: Sonda, monitoramento HTTP e incidentes. Python/FastAPI, interface TypeScript, PostgreSQL.

Cada pasta será um repositório próprio. O diretório pai não será um monorepo. A implementação começa pelo Bancada e avança quando seu fluxo principal, testes e demonstração estiverem verificados. Os demais projetos terão especificações próprias antes de serem implementados. Versões exatas e hospedagem serão escolhidas no plano a partir de suporte oficial, compatibilidade e recursos disponíveis. Nenhum serviço pago será contratado por pressuposição.

## Critérios comuns de publicação

- Fluxo principal completo com persistência real e tratamento de estados vazios, carregamento, validação, erro e sucesso.
- Dados de demonstração fictícios, consistentes e identificados como exemplos.
- Testes de regras críticas, integração com banco e percurso principal no navegador.
- CI com verificações pertinentes à stack, execução local documentada e configurações reproduzíveis.
- Nenhum segredo, dado pessoal real ou material confidencial no repositório.
- README com problema, demonstração, screenshots reais, execução, arquitetura, decisões e limitações verificadas.
- Interface utilizável por teclado, foco visível, contraste legível e adaptação a celular.
- Commits por mudanças compreensíveis, sem histórico fabricado.
- Demonstração pública isolada, com reset de dados e limites de uso definidos antes de expor escrita anônima.
- Roteiro de demonstração e publicação no LinkedIn descrevendo somente funcionalidades verificadas.

Os repositórios atuais não serão alterados nesta entrega: o usuário informou que pretende torná-los privados. Publicação dos novos repositórios faz parte da intenção aprovada; autenticação, hospedagem e confirmação de publicação serão verificadas na etapa correspondente.

## Colaboração entre ferramentas

Codex integra a entrega e responde pela especificação, contratos, revisão e validação. Antigravity recebe tarefas delimitadas de interface e direção visual. Copilot recebe tarefas delimitadas de backend, testes e revisão. Cada tarefa deve registrar objetivo, arquivos permitidos, entradas, critérios de aceite e resultado verificável. Dois agentes não editam os mesmos arquivos simultaneamente. Nenhum resultado é aceito apenas porque um agente declarou conclusão.

Evidência inicial: VS Code e Antigravity IDE estão presentes. VS Code oferece `code chat`; a ajuda do Antigravity IDE também lista `chat`. Acesso autenticado aos agentes e possibilidade de acompanhar resultados ainda precisam ser comprovados. GitHub CLI está autenticado como Diogopbr. O executável dotnet está presente, mas a consulta de SDKs não retornou versões. Estas constatações não comprovam execução de produto nem uso dos agentes.

## Bancada — especificação da primeira entrega

### Problema e público

Uma assistência técnica pequena precisa acompanhar equipamentos, diagnósticos, aprovação de orçamento e entrega sem perder histórico ou executar um reparo sem autorização. Os perfis são atendente, técnico e cliente. Uma instalação atende uma única oficina; múltiplas empresas, cobrança e integração fiscal ficam fora desta primeira entrega.

### Fluxo demonstrável

O atendente registra cliente fictício e equipamento, abre uma ordem e atribui um técnico. O técnico registra diagnóstico e publica um orçamento. O cliente acessa um link limitado àquela ordem e aceita ou rejeita a revisão apresentada. Após aprovação, o técnico inicia e conclui o reparo. O atendente registra entrega. A linha do tempo mostra atores, revisões e horários de todos os eventos.

### Estados e transições

Estados: recebida, em diagnóstico, aguardando aprovação, aprovada, em reparo, pronta para retirada, entregue e cancelada.

- Atendente cria recebida; técnico atribuído inicia diagnóstico.
- Técnico publica orçamento válido e passa para aguardando aprovação.
- Cliente aceita a revisão atual e passa para aprovada; rejeição devolve a ordem para em diagnóstico, registrando a decisão.
- Técnico atribuído inicia reparo apenas após aprovação da revisão atual.
- Técnico conclui reparo e marca pronta para retirada; atendente confirma entrega.
- Atendente pode cancelar antes do início do reparo, com motivo obrigatório.
- Entregue e cancelada são estados terminais.
- Alterar orçamento antes do reparo cria uma nova revisão, invalida aprovação anterior e exige nova aprovação.
- Durante ou após reparo, não se altera orçamento nesta versão.

### Regras críticas

Valores são decimais no servidor e no banco; quantidades são positivas, preços não negativos e o servidor calcula os totais. Orçamento publicado precisa de pelo menos um item e diagnóstico não vazio. Revisões publicadas são imutáveis; itens representam mão de obra ou peça, sem controle de estoque nesta versão.

Todas as transições são autorizadas no servidor. Atualizações incluem controle de versão para impedir que uma tela desatualizada sobrescreva alterações. Publicação de revisão, decisão do cliente, transição de estado e evento correspondente são gravados atomicamente. Repetir a mesma decisão sobre uma revisão já decidida não gera eventos duplicados; tentar decidir uma revisão superada retorna conflito.

Link de cliente usa token aleatório não enumerável, armazenado por hash, com expiração e revogação; é limitado a visualizar aquela ordem e decidir o orçamento atual. Não expõe dados internos nem permite listar clientes. Respostas de token inválido não revelam existência da ordem. Endpoints públicos possuem limitação de requisições. Sessões de equipe usam cookies seguros em produção, proteção contra CSRF para escrita e autorização por perfil; segredos ficam em configuração externa.

### Modelo de dados

Usuário da equipe com perfil; cliente; equipamento vinculado ao cliente; ordem com código legível, estado, técnico e versão; diagnóstico; revisão de orçamento com itens; decisão associada à revisão; acesso do cliente; evento de histórico com ator, instante UTC e ação. Eventos de negócio não serão usados para armazenar tokens ou senhas. Datas são persistidas em UTC e apresentadas de forma localizada.

### Organização técnica

Monólito modular ASP.NET Core com módulos de acesso, ordens, orçamentos e histórico; EF Core e migrations PostgreSQL. Angular organizado por funcionalidades de fila, detalhe de ordem, entrada e acompanhamento do cliente. API com contrato OpenAPI, validação no servidor e erros estruturados, incluindo conflito de versão. O frontend não replica decisões de autorização como fonte de verdade. Não introduzir microserviços, event sourcing ou filas sem necessidade demonstrada.

### Telas e direção visual

Fila de trabalho com busca por código/equipamento, filtro por estado e técnico; abertura da ordem com validação; detalhe com ficha do equipamento, diagnóstico, orçamento e linha do tempo; acompanhamento do cliente com orçamento legível e decisão explícita; entrada da equipe.

Direção: ferramenta de oficina com fundo claro quente, texto escuro, cor de ação controlada, códigos de ordem destacados e hierarquia tipográfica editorial. Estados usam texto e sinais visuais além de cor. Priorizar listas úteis, formulários claros e histórico; métricas só entram quando contribuírem para uma ação. Textos em português brasileiro. O orçamento deve permanecer confortável de ler e aprovar em celular.

### Dados de demonstração

Uma oficina fictícia com atendente e dois técnicos, equipamentos variados e ordens em diferentes estados. Incluir uma ordem pronta para diagnóstico, uma aguardando aprovação, uma com revisão superada e uma entregue. O percurso guiado cria uma ordem nova; os exemplos existentes explicam estados sem depender de histórico inventado como experiência comercial.

### Verificação e aceite

1. Executar o percurso de recebimento até entrega no navegador, incluindo aprovação pela visão do cliente.
2. Provar que técnico não inicia reparo sem aprovação e que atendente/cliente não executam ações do técnico.
3. Provar que revisão nova invalida aprovação anterior e que a antiga não pode ser aprovada.
4. Provar cálculo monetário, rejeição de quantidades inválidas e preservação de revisões.
5. Provar conflito entre duas atualizações concorrentes e ausência de evento duplicado em repetição da decisão.
6. Provar persistência após reinício da aplicação e migrations em banco vazio.
7. Provar expiração/revogação de acesso do cliente e isolamento entre ordens.
8. Verificar formulário, orçamento e ações principais por teclado e em viewport móvel.
9. Executar build, análise estática e testes pertinentes; registrar separadamente verificações locais, CI e demonstração hospedada.

### Limites da primeira versão

Sem pagamentos reais, emissão fiscal, estoque, anexos, envio de email/WhatsApp, múltiplas oficinas ou integrações externas. O link de aprovação é apresentado no fluxo de demonstração. A infraestrutura da demo, retenção/reset e disponibilização segura de contas serão definidas no plano antes de publicação.

## Próxima etapa

Revisar esta especificação. Após aprovação, produzir plano de implementação do Bancada com instalação de dependências necessárias, contratos, tarefas separadas para ferramentas, testes e critérios de integração/publicação. Conceitos dos outros três produtos permanecem aprovados, mas não substituem suas futuras especificações.

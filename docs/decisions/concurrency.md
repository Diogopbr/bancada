# Concorrência nas ordens

Cada ordem tem uma versão. O cliente envia `expectedVersion` em uma alteração e recebe HTTP 409 quando essa versão já foi superada. Assim, uma aba antiga não sobrescreve silenciosamente o trabalho realizado em outra.

A API abre uma transação e obtém um lock PostgreSQL `FOR UPDATE` na ordem. Depois de obter o lock, confere a versão e a permissão, aplica a mudança e grava seu evento. A versão também é um token de concorrência do EF Core.

A publicação de orçamento, a decisão do cliente e a alteração do acesso compartilham o lock da ordem. A validade do acesso é conferida depois de obter o lock, impedindo que uma decisão aguardando liberação use um acesso já revogado.

Uma decisão igual à já persistida é idempotente. Uma decisão oposta ou referente a uma revisão superada retorna conflito. Aprovar novamente não adiciona um segundo evento.

Os testes de integração usam bancos PostgreSQL exclusivos. A execução independente de 10/10/2026 aprovou 44 testes, incluindo duas alterações com a mesma versão e duas decisões opostas concorrentes: em cada caso, exatamente uma operação foi aceita e um evento foi persistido. O percurso Playwright da API também verificou duas tentativas de iniciar o reparo com a mesma versão, com respostas 200 e 409.

Esses resultados comprovam o comportamento coberto pelos cenários executados. Ainda não representam um ensaio de carga nem uma medição de capacidade da hospedagem.

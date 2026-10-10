# Antigravity — demo e teclado, validação pelo Codex

O Antigravity CLI implementou os controles de demonstração, entrada sem senha compartilhada quando habilitada, troca de três papéis, confirmação de reset, tratamento de expiração/quota e proteção CSRF depois da troca de identidade. Também adicionou uma diretiva de foco para os diálogos de decisão do cliente, cancelamento e reset, com Tab/Shift+Tab, Escape e restauração de foco.

O acesso do cliente usa apenas token no fragmento; a alternativa por query foi removida. Textos de rejeição indicam decisão registrada, sem alegar envio de email.

A execução headless terminou com RunCommand recusado por falta de uma regra de permissão correspondente. Portanto, o status SUCCESS do CLI não foi considerado prova de conclusão. Codex executou os comandos abaixo diretamente na worktree, em 10/10/2026:

- `npm test -- --watch=false`: 8 arquivos, 33 testes aprovados, zero falhas.
- `npm run build`: exit code 0, build Angular válido, bundle inicial 296.26 kB.

Não foi confirmado RED/GREEN dos novos testes da demo pelo CLI; a evidência acima é a execução independente do estado entregue. Integração com API real, reset no navegador, teclado real, screenshot e hospedagem continuam sob responsabilidade do Codex.

Escopo implementado: frontend e testes Angular. Nenhum backend, commit, push ou deploy foi executado pelo Antigravity. A integração/commit é realizada pelo Codex depois dessa verificação.

# Bancada — rascunho para LinkedIn

Texto preparado; ainda não publicado.

Construí o Bancada para explorar um problema de assistência técnica: como garantir que o reparo autorizado pelo cliente corresponde à versão certa do orçamento?

O fluxo vai da entrada do equipamento à entrega. O técnico registra o diagnóstico e publica um orçamento; o cliente aprova por um link limitado àquela ordem. Se o orçamento muda, a aprovação anterior perde validade. Se duas abas tentam alterar a mesma ordem, a API recusa a atualização desatualizada.

Usei Angular, ASP.NET Core e PostgreSQL. A demonstração tem dados fictícios separados por visitante e permite alternar entre atendente e técnicos, acompanhar o cliente e reiniciar o ambiente.

Além da interface, trabalhei autorização no servidor, transações, valores decimais, histórico, sessões, CSRF e testes com banco real. O repositório tem 65 testes de backend, 36 de frontend e seis cenários de navegador, com CI no GitHub Actions. Também verifiquei o percurso publicado na Vercel com Neon.

Desenvolvi com apoio de Codex, Copilot e Antigravity. Integrei e revisei as entregas com testes independentes e corrigi os problemas encontrados no navegador.

Demo: https://bancada-seven.vercel.app
Código e decisões: https://github.com/Diogopbr/bancada

#Angular #DotNet #PostgreSQL #DesenvolvimentoWeb
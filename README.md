# Bancada

Operação de assistência técnica: recebimento, diagnóstico, orçamento, aprovação do cliente, reparo e entrega.

Projeto de portfólio em implementação por Diogo Pereira. Os dados serão fictícios. Não há demonstração pública nesta etapa.

## Stack

Angular 21, ASP.NET Core/.NET 10, EF Core e PostgreSQL 17. Interface em português brasileiro; backend responsável por autorização, cálculo monetário e transições.

## Desenvolvimento local no Windows

Pré-requisitos: Node 24, npm, SDK .NET 10.0.401 e binários PostgreSQL 17. Nesta máquina o SDK está em `D:/Eu/Projetos/.tools/dotnet`; scripts detectam essa instalação local. Em outra máquina, use SDK no PATH ou configure DOTNET_ROOT.

Execute `./scripts/bootstrap.ps1` para preparar uma instância PostgreSQL isolada na porta 55417; não altera o serviço existente. Credenciais locais aleatórias e dados ficam em `.local/`, ignorado pelo Git. Instalação PostgreSQL em outro caminho aceita `-PostgresBin`.

Execute `npm ci` em `frontend/`, depois `./scripts/dev.ps1` na raiz. Interface: http://localhost:4200. API: http://localhost:5080/health/ready. A interface inicial ainda é a base Angular; as telas de produto estão em desenvolvimento.

Execute `./scripts/verify.ps1` para build e testes locais. Testes de banco precisam de conexão real; não são ignorados se o banco estiver indisponível.

## Documentação

- [Especificação](docs/superpowers/specs/2026-10-10-portfolio-design.md)
- [Plano](docs/superpowers/plans/2026-10-10-bancada.md)
- [Contrato HTTP](docs/contracts/http.md)
- [Evidência de execução](docs/evidence/progress.md)

Publicação visual planejada na Vercel. A compatibilidade e o funcionamento do backend hospedado serão verificados antes de anunciar a demonstração.

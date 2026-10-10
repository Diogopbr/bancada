# Publicação na Vercel

O usuário escolheu Vercel para disponibilizar o visual dos projetos. O Bancada mantém Angular e ASP.NET Core no mesmo domínio para sessão e CSRF, com PostgreSQL externo.

Em 10/10/2026, a documentação oficial descreve Container Runtime beta em todos os planos, com detecção de Dockerfile.vercel. A imagem proposta empacota o Angular em wwwroot junto da API. A presença do Dockerfile não comprova um deploy: acesso ao runtime, build, HTTPS, cookies, conexão ao banco e fluxos precisam ser verificados no ambiente publicado.

Instâncias podem escalar e perder disco local. Persistência do PostgreSQL e do key ring Data Protection precisa ser compartilhada. A configuração de produção não pode confiar em key ring local do Windows ou no filesystem do contêiner. Requisições atrás do proxy precisam considerar HTTPS de forma controlada.

O PostgreSQL local não será exposto na internet. Provisionamento externo terá configuração secreta e limite de custo conhecido. Não substituir backend por dados fictícios no navegador sem identificar explicitamente uma eventual prévia visual.

Referências:

- https://vercel.com/docs/functions/runtimes/container
- https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0
# Integração do proxy

Em um container acessível exclusivamente pelo gateway da Vercel, `Hosting__VercelIngress=true` exige `VERCEL=1` e usa `x-vercel-forwarded-for` com um salto. Esse modo depende do isolamento do runtime e dos headers controlados pela plataforma; não deve ser habilitado em um servidor Kestrel exposto diretamente. Hospedagem própria exige IPs/redes de proxy explícitos. Os [headers oficiais da Vercel](https://vercel.com/docs/headers/request-headers) documentam a substituição de IP de origem para impedir spoofing.


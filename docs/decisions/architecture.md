# Arquitetura do Bancada

Bancada acompanha uma ordem de assistência técnica do recebimento à entrega. A aplicação reúne interface Angular e API ASP.NET Core em um domínio, com PostgreSQL como fonte dos dados de negócio.

O backend é um monólito organizado em acesso, ordens, orçamentos e histórico. Esse formato permite concluir cada mudança de estado e seu evento na mesma transação. A oficina não precisa de serviços distribuídos para executar esse fluxo.

Em desenvolvimento, o Angular encaminha `/api` para a API local. O empacotamento de produção reúne os arquivos compilados e o backend. A publicação desse empacotamento ainda precisa de verificação no ambiente hospedado.

A equipe usa ASP.NET Core Identity, cookie HttpOnly, proteção CSRF e autorização no servidor. O técnico responsável pode diagnosticar, publicar orçamento e executar o reparo; o atendente recebe, atribui e entrega o equipamento. A interface apresenta as ações disponíveis, enquanto a API decide se a operação é permitida.

O cliente possui um acesso limitado a uma ordem. O token aleatório é enviado no fragmento do link e trocado por um cookie próprio. O banco armazena o hash do token. A projeção pública omite email, diagnóstico e notas internas. Expiração, revogação e substituição são conferidas novamente na API.

Cada orçamento publicado é uma revisão imutável. A API recebe preços como texto decimal com até duas casas, calcula o total em `decimal` e persiste valores decimais. Uma nova revisão exige nova aprovação. A decisão repetida retorna o resultado existente sem duplicar eventos.

O modo de demonstração pública está em implementação. Seu contrato exige um conjunto privado de dados por visitante, limite de 20 ordens e validade de 24 horas; essa funcionalidade será anunciada somente depois dos testes de isolamento e da verificação hospedada.

Pagamentos, estoque, emissão fiscal, anexos e envio de mensagens ficam fora desta versão.

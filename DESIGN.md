---
name: Bancada
description: Ficha de serviço digital para assistência técnica
colors:
  page: "#f0f2f2"
  surface: "#ffffff"
  surface-subtle: "#f7f8f8"
  sidebar: "#152121"
  ink: "#182323"
  ink-muted: "#526060"
  border: "#d0d7d7"
  border-subtle: "#e2e7e7"
  primary: "#243737"
  primary-hover: "#182b2b"
  primary-subtle: "#eaf0f0"
  accent: "#b33b1d"
  accent-hover: "#922f17"
  accent-subtle: "#fdf3ef"
typography:
  display:
    fontFamily: "Barlow Condensed, sans-serif"
    fontSize: "clamp(3.5rem, 5.5vw, 6rem)"
    fontWeight: 600
    lineHeight: 0.98
  headline:
    fontFamily: "Barlow Condensed, sans-serif"
    fontSize: "2.65rem"
    fontWeight: 600
    lineHeight: 1.25
    letterSpacing: "-0.02em"
  title:
    fontFamily: "Barlow Condensed, sans-serif"
    fontSize: "1.6rem"
    fontWeight: 600
    lineHeight: 1.25
  body:
    fontFamily: "Barlow, system-ui, sans-serif"
    fontSize: "15px"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Barlow, system-ui, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 600
  code:
    fontFamily: "ui-monospace, Cascadia Code, Source Code Pro, Menlo, Consolas, monospace"
rounded:
  control: "2px"
spacing:
  compact: "0.5rem"
  field: "0.75rem"
  regular: "1rem"
  panel: "1.25rem"
  section: "1.5rem"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    rounded: "{rounded.control}"
    padding: "0.5rem 1rem"
  button-primary-hover:
    backgroundColor: "{colors.primary-hover}"
  button-accent:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.surface}"
    rounded: "{rounded.control}"
    padding: "0.5rem 1rem"
  button-accent-hover:
    backgroundColor: "{colors.accent-hover}"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "0.5rem 1rem"
  input:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "0.5rem 0.75rem"
  panel:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.control}"
    padding: "1.25rem"
---

# Design System: Bancada

## Overview

**Creative North Star: "Ficha de serviço e instrumento de oficina"**

Uma interface operacional firme, com superfícies frias, leitura direta e ações identificáveis. O caráter vem da tipografia condensada, da organização do trabalho e do contraste entre grafite e laranja escuro. A metáfora orienta a informação; não reproduz papel envelhecido, instrumentos físicos ou textura artificial.

A direção e a execução code-first foram escolhidas por Codex dentro da autonomia delegada pelo usuário em 10/10/2026, após a rejeição do visual genérico. Não houve concept roll, seed, mock aprovado ou quality-bar card separado. A referência de qualidade adotada foi leitura longa na oficina, hierarquia dos dados e ações, navegação por teclado e uso no celular. Nenhuma escolha estética pessoal é atribuída ao usuário.

**Key Characteristics:**

- Superfícies claras frias e navegação em grafite.
- Títulos condensados, texto de leitura em Barlow e códigos monoespaçados.
- Seções contínuas de ficha, separadas por linhas.
- Ações, orçamento e estados reais preservados na hierarquia.

## Colors

Grafite sustenta leitura e navegação; o laranja escuro sinaliza ações de destaque. Os valores normativos estão no frontmatter e correspondem aos estilos atuais.

### Primary

- **Grafite de ação:** botões principais internos e identificação de elementos operacionais; hover mais profundo.
- **Laranja de sinal:** entrada da demonstração, links e total do orçamento do cliente; hover mais escuro e superfície clara de apoio.

### Neutral

- **Cinza de trabalho:** fundo da aplicação.
- **Branco de ficha:** formulários, tabelas e superfícies de leitura.
- **Cinza de seção:** cabeçalhos de tabela e áreas de apoio.
- **Grafite de navegação:** barra lateral e apresentação da entrada.
- **Tinta e tinta secundária:** conteúdo principal e metadados.
- **Linhas de estrutura:** bordas e divisórias; a variante sutil separa linhas internas.

**The Sinal de Ação Rule.** Use o laranja para ação ou informação decisiva; não transforme cada seção em um bloco de destaque.

As cores semânticas de sucesso, erro e fluxo pertencem ao estado real. A coleção multicolorida herdada dos status não estabelece acentos de marca adicionais.

## Typography

**Display Font:** Barlow Condensed, com fallback sans-serif.
**Body Font:** Barlow, com fallback system-ui e sans-serif.
**Label/Mono Font:** pilha monoespaçada do frontmatter para códigos e valores.

As duas fontes são locais, com font-display swap e licença OFL em public/fonts. Barlow Condensed dá direção aos títulos; Barlow mantém a leitura dos campos e descrições. O corpo base tem line-height de 1.5. Números de tabelas e códigos usam alinhamento tabular.

### Hierarchy

- **Display:** mensagem de entrada, com composição fluida; no celular passa a 3.3rem.
- **Headline:** título da fila de trabalho. O título de acesso usa 2.8rem, reduzido a 2.25rem no celular.
- **Title:** seções de diagnóstico, equipamento e orçamento.
- **Body:** texto contínuo e base da aplicação; campos e tabelas usam variações compactas observadas.
- **Label:** identificação dos campos; não atua como título decorativo.
- **Code:** ordem de serviço, revisão e valores que exigem comparação.

**The Tipo com Função Rule.** Reserve a condensada para títulos e identidade; não transfira descrições longas para a fonte de display.

## Layout

A área operacional tem navegação lateral de 216px e conteúdo com padding de 2rem por 2.5rem. Fila e detalhe limitam a largura a 1200px. Abaixo de 768px a navegação vira barra superior, o padding principal cai para 1rem e a fila vira uma lista vertical de ordens.

O detalhe distribui ficha e apoio em colunas de 2fr/1fr, com intervalo de 1.5rem. Abaixo de 900px as colunas se empilham. A ficha principal é uma superfície contínua com seções de 1.5rem, reduzidas a 1.15rem em telas menores. A tabela de orçamento conserva colunas numéricas e rolagem horizontal delimitada; o aviso de rolagem aparece abaixo de 600px.

A entrada divide apresentação e acesso em 1.15fr/1fr, ocupando ao menos 100dvh. Abaixo de 760px as áreas se empilham. A ficha do cliente tem largura máxima de 640px, padding de 1.75rem e redução para 1.25rem abaixo de 480px; as decisões passam a coluna no mesmo breakpoint.

## Elevation & Depth

As superfícies operacionais usam contraste tonal e linhas, sem sombras decorativas. A ficha principal comunica continuidade; o histórico fica em uma superfície secundária. O modal atual mantém uma sombra difusa para separação do plano sobre o backdrop, sem efeito de objeto físico. A ausência de sombra nos painéis é deliberada e não uma licença para remover a hierarquia.

**The Ficha Contínua Rule.** Separe seções relacionadas com linhas e espaço; não repita cartões idênticos para cada par de rótulo e valor.

## Shapes

Controles e painéis usam cantos discretos de 2px. Essa escolha deliberada de instrumento digital substitui um preset genérico de cartões arredondados. Seções internas da ficha não ganham cantos próprios. Pontos de histórico e indicadores de estado podem ser circulares porque comunicam eventos e condição, não um novo formato de cartão.

## Components

### Buttons

Botões compactos e legíveis, com texto de ação. O principal interno usa grafite; o destaque usa laranja; o secundário usa fundo branco e borda. Os estados hover mudam cor em 150ms, e disabled reduz opacidade e altera o cursor. A entrada da demonstração tem altura mínima de 48px. O foco visível global usa contorno de 2px no acento, com deslocamento de 2px.

### Chips

Status usam texto, ponto e superfície semântica, com raio final de 2px. Revisão e código são metadados compactos. Cor nunca substitui o nome do estado.

### Cards / Containers

Painéis de apoio têm borda, fundo branco e padding de 1.25rem. A ficha principal agrupa seções contínuas; o cliente recebe uma única superfície de leitura. Não aplicar uma sombra de cartão a cada seção.

### Inputs / Fields

Campos brancos com borda neutra, padding de 0.5rem por 0.75rem e fonte de leitura. O foco muda a borda para grafite e mantém o contorno global. Erros ficam vinculados ao campo em texto visível. Áreas de texto permitem redimensionamento vertical.

### Navigation

Navegação lateral grafite, links claros e estado ativo preenchido. Hover e foco permanecem identificáveis. No celular, os links passam à barra superior; o conteúdo não depende de hover.

### Ficha e orçamento

O código e o estado identificam a ordem antes da ficha. Diagnóstico, equipamento e orçamento permanecem seções relacionadas. Valores de orçamento alinham à direita com números tabulares; o total do cliente recebe destaque em laranja. O histórico conserva eventos, ator e data como informação secundária.

## Do's and Don'ts

### Do:

- **Do** preservar fonte local, hierarquia de títulos e números tabulares.
- **Do** usar linhas e agrupamento para tornar a ficha contínua.
- **Do** manter rótulos, foco visível e ações disponíveis sem hover.
- **Do** respeitar prefers-reduced-motion nas transições e animações.

### Don't:

- **Don't** inventar textura física, métricas, clientes ou resultados para decorar a interface.
- **Don't** transformar rótulos operacionais em chamadas decorativas acima dos títulos.
- **Don't** promover cores herdadas de status a novos acentos de marca.
- **Don't** reintroduzir cartões iguais em cada seção da ficha.

Registro de drift não canonizado: alguns seletores herdados ainda usam pesos sem arquivo dedicado, verde residual em metadados, hover bege da tabela e círculo com glifo no estado de erro. Eles descrevem pendências do código, não regras para novas telas. Esta passagem documenta o sistema e não altera a interface.

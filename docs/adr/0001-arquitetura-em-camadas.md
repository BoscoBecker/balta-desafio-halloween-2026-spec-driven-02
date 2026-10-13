# ADR 0001: Arquitetura em camadas

- Status: Aceita
- Data: 2026-10-13

## Contexto

A API precisa expor os fluxos HTTP sem misturar regras de negócio com detalhes de transporte ou persistência. Essa separação também permite testar as regras sem hospedar a API ou acessar o banco.

## Decisão

Organizar a solução em camadas: `UrlShortener.Api` mapeia os endpoints Minimal API e traduz requisições/respostas; `UrlShortener.Application` concentra os casos de uso em `LinkService` e define abstrações; `UrlShortener.Domain` representa o agregado `Link`; `UrlShortener.Infrastructure` implementa persistência, cache e gerenciamento de chaves. Os endpoints chamam a aplicação, e não acessam o banco diretamente.

## Consequências

- Regras de negócio podem ser testadas isoladamente e dependem de abstrações.
- Adaptadores de infraestrutura podem ser substituídos sem mover regras para a API.
- A solução tem mais projetos e interfaces do que uma implementação monolítica simples.

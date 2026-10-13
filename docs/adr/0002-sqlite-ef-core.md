# ADR 0002: SQLite e Entity Framework Core

- Status: Aceita
- Data: 2026-10-13

## Contexto

Os links e suas estatísticas precisam continuar disponíveis após reinícios da aplicação. A solução também precisa de um armazenamento simples para execução local e testes de integração.

## Decisão

Persistir os links em SQLite usando Entity Framework Core. `LinkRepository` implementa `ILinkRepository`, e as alterações de esquema são aplicadas por migrations.

## Consequências

- Os dados sobrevivem ao reinício da aplicação sem exigir um servidor de banco separado.
- O modelo e as alterações de esquema ficam versionados em migrations.
- SQLite atende ao escopo atual; requisitos de escala ou disponibilidade podem exigir outro provedor.

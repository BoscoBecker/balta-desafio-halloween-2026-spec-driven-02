# ADR 0003: Contagem atômica de acessos

- Status: Aceita
- Data: 2026-10-13

## Contexto

Acessos simultâneos ao mesmo link não podem sobrescrever incrementos do contador. Ler o valor para a aplicação, somar um e gravar de volta pode perder atualizações concorrentes.

## Decisão

Incrementar `AccessCount` com um `UPDATE` executado diretamente pelo banco via `ExecuteUpdateAsync`, condicionado ao código e à validade do link.

## Consequências

- O banco serializa a atualização do contador sem o ciclo de leitura e gravação na aplicação.
- Cada redirecionamento ainda realiza uma atualização persistente, inclusive quando o destino está no cache.
- A operação fica vinculada aos recursos de atualização em lote do EF Core.

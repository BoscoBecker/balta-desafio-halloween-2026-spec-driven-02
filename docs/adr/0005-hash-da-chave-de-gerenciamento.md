# ADR 0005: Hash da chave de gerenciamento

- Status: Aceita
- Data: 2026-10-13

## Contexto

A chave de gerenciamento autoriza a leitura das estatísticas de um link. Armazená-la em texto puro permitiria que uma cópia do banco expusesse as credenciais diretamente.

## Decisão

Gerar uma chave aleatória para ser apresentada uma única vez na resposta de criação e persistir somente seu hash. Ao consultar estatísticas, comparar a chave recebida com o hash armazenado.

## Consequências

- Uma cópia do banco não revela diretamente as chaves válidas.
- A chave original não pode ser recuperada pelo servidor; o cliente precisa guardar a resposta da criação.
- O endpoint de estatísticas exige o cabeçalho `X-Management-Key`.

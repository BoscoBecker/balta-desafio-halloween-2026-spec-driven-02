# ADR 0006: Cache local de redirecionamentos

- Status: Aceita
- Data: 2026-10-13

## Contexto

Redirecionamentos são o caminho de leitura mais frequente. Buscar o destino no banco a cada requisição adiciona latência, mas o contador de acessos precisa permanecer correto e persistente.

## Decisão

Manter em cache em memória o destino e a expiração dos links consultados, com expiração deslizante de cinco minutos. O cache não armazena o contador: cada redirecionamento incrementa o contador atomicamente no repositório.

## Consequências

- Leituras repetidas podem evitar buscar os dados do link no banco.
- Reiniciar a aplicação ou remover uma entrada do cache não perde links nem contagens.
- O cache é local ao processo; múltiplas instâncias não compartilham entradas, mas continuam usando o banco como fonte de verdade.

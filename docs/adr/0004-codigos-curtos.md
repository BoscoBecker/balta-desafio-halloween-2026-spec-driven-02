# ADR 0004: Códigos curtos únicos e legíveis

- Status: Aceita
- Data: 2026-10-13

## Contexto

Os códigos precisam ser curtos, fáceis de compartilhar e únicos, mesmo quando várias requisições tentam criar links ao mesmo tempo.

## Decisão

Gerar códigos automáticos com um alfabeto que evita caracteres ambíguos. Validar códigos personalizados e gerados na aplicação, e impor um índice único no banco como garantia final contra corridas. Em conflito de código gerado, tentar outro código até o limite configurado.

## Consequências

- Códigos ficam mais fáceis de ler e ditar.
- O índice do banco é a autoridade para unicidade concorrente; a checagem prévia sozinha não seria suficiente.
- A geração pode falhar temporariamente se todas as tentativas limitadas colidirem.

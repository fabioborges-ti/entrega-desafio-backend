# ADR-0002 — Rating como recurso próprio

- Status: Aceito
- Data: 2026-04-27

## Contexto

A referência original de produtos sugere `rating` no contrato de escrita (`POST` e `PUT`), incluindo média e contagem.

No domínio, média e contagem são informações agregadas, não atributos autoritativos para cadastro/edição administrativa de produto.

## Decisão

Não aceitar `rating` no payload de criação/edição de produto. Tratar avaliação como recurso próprio em `POST /api/products/{id}/ratings`, com agregação calculada na leitura.

## Alternativas consideradas

### Aceitar `rating` em `POST` e `PUT`

- Prós: aderência literal ao formato de payload de referência.
- Contras: permite informar média/contagem sem avaliações reais e reduz auditabilidade.

### Calcular rating em job assíncrono de agregação

- Prós: melhor performance para leitura em cenários de alto volume.
- Contras: aumenta complexidade operacional sem necessidade imediata do desafio.

## Consequências

### Positivas

- Evita manipulação manual de indicadores agregados.
- Aumenta coerência semântica do contrato (cadastrar produto != avaliar produto).
- Facilita aplicar regras de autorização por tipo de ação.

### Negativas

- Introduz endpoint adicional para avaliações.
- Demanda comunicação clara no README para consumidores da API.

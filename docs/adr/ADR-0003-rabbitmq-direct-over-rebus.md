# ADR-0003 — RabbitMQ direto em vez de Rebus

- Status: Aceito
- Data: 2026-04-27

## Contexto

O projeto já possui fluxo assíncrono de vendas funcional com RabbitMQ, incluindo topologia explícita, retry, DLQ e rastreamento por `correlationId`.

Foi considerada a adoção de um framework de mensageria (Rebus) para abstração de handlers e políticas.

## Decisão

Manter a integração direta com RabbitMQ neste estágio do projeto.

## Alternativas consideradas

### Adotar Rebus imediatamente

- Prós: abstração de infraestrutura e menor código manual de mensageria.
- Contras: custo de migração, nova camada conceitual e risco de concorrência/duplicidade com consumidores existentes durante transição.

### Manter RabbitMQ direto (opção escolhida)

- Prós: transparência das decisões para avaliação técnica, menor complexidade no escopo atual.
- Contras: maior volume de código de infraestrutura próprio.

## Consequências

### Positivas

- Clareza operacional da topologia (exchange, filas, routing keys, DLQ).
- Menos dependências e menor superfície de mudança no curto prazo.
- Facilita revisão técnica do desafio.

### Negativas

- Menos abstração para escalar novos fluxos assíncronos.
- Possível retrabalho em futura migração para framework de mensageria.

## Revisão futura

Reavaliar se o número de fluxos assíncronos crescer, quando houver ganho claro em padronização e manutenção com framework dedicado.

## Adequação ao escopo atual

O projeto possui escopo controlado e um fluxo assíncrono principal (vendas). Nesse cenário, a integração direta com RabbitMQ atende os requisitos com menor complexidade operacional e maior transparência para revisão técnica.

Neste estágio, adotar Rebus adicionaria abstração sem ganho proporcional imediato. A migração pode ser reavaliada se houver crescimento do número de fluxos assíncronos, consumidores e necessidade de padronização transversal de mensageria.

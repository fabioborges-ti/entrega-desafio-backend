# ADR-0001 — Venda nasce de carrinho

- Status: Aceito
- Data: 2026-04-27

## Contexto

O desafio exige que a API de vendas retorne dados completos (itens, preços, descontos e totais), mas não define de forma explícita um payload autoritativo para criação/atualização de venda.

Sem uma decisão clara, o cliente poderia enviar dados derivados (`unitPrice`, `discount`, `totalAmount`) como se fossem fonte de verdade, abrindo espaço para inconsistência entre catálogo, carrinho, inventário e venda.

## Decisão

Criar e atualizar venda a partir de um carrinho previamente validado, recebendo referências (`CustomerId`, `BranchId`, `CartId`) e derivando itens, preços, descontos e totais no domínio.

## Alternativas consideradas

### Permitir payload completo da venda

- Prós: menos etapas para o consumidor da API.
- Contras: maior risco de manipulação de preço/desconto/total e divergências de negócio.

### Criar venda sem carrinho e calcular tudo com produtos diretos

- Prós: fluxo de venda independente.
- Contras: duplicação de regras de validação/reserva já existentes no carrinho e maior acoplamento no caso de uso de vendas.

## Consequências

### Positivas

- Preserva consistência de domínio.
- Mantém fonte de verdade clara (catálogo + carrinho + política de desconto).
- Reforça rastreabilidade do fluxo de compra.

### Negativas

- Exige etapa prévia de criação/atualização de carrinho.
- Aumenta número de chamadas no fluxo cliente -> API.

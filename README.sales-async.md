# Sales Async Use Case (RabbitMQ)

[Back to main README](./README.md)

Este documento descreve o fluxo assíncrono do caso de uso de vendas (`Sales`) com RabbitMQ para os cenários:

- `SaleCreated`
- `SaleModified`
- `SaleCancelled`
- `SaleDeleted`

## Objetivo

Permitir que os endpoints de vendas respondam rapidamente (`202 Accepted`) e deleguem a persistência para processamento assíncrono via mensageria.

## Topologia RabbitMQ

- Exchange principal (`topic`): `devstore.sales.events.v1`
- Routing keys:
  - `sale.created.v1`
  - `sale.modified.v1`
  - `sale.cancelled.v1`
  - `sale.deleted.v1`
- Filas de processamento:
  - `devstore.sales.created.persist.v1`
  - `devstore.sales.modified.persist.v1`
  - `devstore.sales.cancelled.persist.v1`
  - `devstore.sales.deleted.persist.v1`
- Dead letter:
  - Exchange: `devstore.sales.dlx.v1`
  - Queue: `devstore.sales.dlq.v1`

## Fluxo ponta a ponta

1. API recebe requisição de create/update/cancel/delete.
2. Request é validada no controller.
3. Producer publica envelope no RabbitMQ com `correlationId`.
4. API retorna `202 Accepted` com `correlationId`.
5. Consumer lê da fila e executa validações de negócio + persistência via handlers/repositórios.
6. Em sucesso: `ack`.
7. Em falha: retry com backoff explícito; ao exceder tentativas, mensagem segue para DLQ.

## Contrato de resposta assíncrona

Nos endpoints assíncronos de Sales, o retorno segue o padrão:

```json
{
  "success": true,
  "message": "Solicitação ... enfileirada com sucesso",
  "data": {
    "correlationId": "abc123..."
  }
}
```

## Consulta de status por correlationId

Endpoint:

- `GET /api/sales/messages/{correlationId}`

Estados possíveis:

- `Queued`
- `Processing`
- `Retrying`
- `Succeeded`
- `DeadLettered`

## Retry com backoff explícito

Configuração em `appsettings`:

```json
"SalesMessaging": {
  "Retry": {
    "MaxRetries": 3,
    "BackoffSeconds": [2, 5, 15]
  }
}
```

Comportamento:

- falha na tentativa 1 -> espera 2s e republica
- falha na tentativa 2 -> espera 5s e republica
- falha na tentativa 3 -> espera 15s e republica
- excedeu `MaxRetries` -> `nack` sem requeue (DLQ)

## Como validar rapidamente

1. Subir stack com RabbitMQ + DB.
2. Chamar um endpoint de Sales assíncrono (create/update/cancel/delete).
3. Confirmar `202 Accepted` e capturar `correlationId`.
4. Consultar `GET /api/sales/messages/{correlationId}`.
5. Verificar, no final, `Succeeded` (ou `DeadLettered` em caso de falha permanente).

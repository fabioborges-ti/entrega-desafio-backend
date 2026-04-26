using System.Text.Json;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public sealed record SaleMessageEnvelope(
    string EventName,
    string CorrelationId,
    DateTime OccurredAtUtc,
    JsonElement Payload);

using System.Collections.Concurrent;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public sealed class InMemorySalesMessageStatusStore : ISalesMessageStatusStore
{
    private readonly ConcurrentDictionary<string, SalesMessageStatus> _statuses = new();

    public void MarkQueued(string correlationId, string eventName, string? payloadJson = null)
    {
        var now = DateTime.UtcNow;
        _statuses[correlationId] = new SalesMessageStatus
        {
            CorrelationId = correlationId,
            EventName = eventName,
            State = SalesMessageProcessingState.Queued,
            Attempts = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PayloadJson = payloadJson
        };
    }

    public void MarkProcessing(string correlationId, string eventName, int attempts)
    {
        Upsert(correlationId, eventName, status =>
        {
            status.State = SalesMessageProcessingState.Processing;
            status.Attempts = attempts;
            status.LastError = null;
            status.NextRetryInSeconds = null;
        });
    }

    public void MarkRetrying(string correlationId, string eventName, int attempts, TimeSpan nextRetryIn, string error)
    {
        Upsert(correlationId, eventName, status =>
        {
            status.State = SalesMessageProcessingState.Retrying;
            status.Attempts = attempts;
            status.LastError = error;
            status.NextRetryInSeconds = (int)Math.Ceiling(nextRetryIn.TotalSeconds);
        });
    }

    public void MarkSucceeded(string correlationId, string eventName, int attempts)
    {
        Upsert(correlationId, eventName, status =>
        {
            status.State = SalesMessageProcessingState.Succeeded;
            status.Attempts = attempts;
            status.LastError = null;
            status.NextRetryInSeconds = null;
        });
    }

    public void MarkDeadLettered(string correlationId, string eventName, int attempts, string error)
    {
        Upsert(correlationId, eventName, status =>
        {
            status.State = SalesMessageProcessingState.DeadLettered;
            status.Attempts = attempts;
            status.LastError = error;
            status.NextRetryInSeconds = null;
        });
    }

    public bool TryGet(string correlationId, out SalesMessageStatus? status) =>
        _statuses.TryGetValue(correlationId, out status);

    private void Upsert(string correlationId, string eventName, Action<SalesMessageStatus> mutator)
    {
        var now = DateTime.UtcNow;
        _statuses.AddOrUpdate(
            correlationId,
            _ =>
            {
                var status = new SalesMessageStatus
                {
                    CorrelationId = correlationId,
                    EventName = eventName,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                mutator(status);
                return status;
            },
            (_, status) =>
            {
                status.EventName = eventName;
                mutator(status);
                status.UpdatedAtUtc = now;
                return status;
            });
    }
}

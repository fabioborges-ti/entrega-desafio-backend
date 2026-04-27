using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public sealed class PostgresSalesMessageStatusStore : ISalesMessageStatusStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresSalesMessageStatusStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void MarkQueued(string correlationId, string eventName, string? payloadJson = null)
    {
        var now = DateTime.UtcNow;
        Upsert(correlationId, eventName, record =>
        {
            record.State = SalesMessageProcessingState.Queued.ToString();
            record.Attempts = 0;
            record.LastError = null;
            record.NextRetryInSeconds = null;
            record.PayloadJson = payloadJson;
            record.CreatedAtUtc = now;
            record.UpdatedAtUtc = now;
        });
    }

    public void MarkProcessing(string correlationId, string eventName, int attempts)
    {
        Upsert(correlationId, eventName, record =>
        {
            record.State = SalesMessageProcessingState.Processing.ToString();
            record.Attempts = attempts;
            record.LastError = null;
            record.NextRetryInSeconds = null;
            record.UpdatedAtUtc = DateTime.UtcNow;
        });
    }

    public void MarkRetrying(string correlationId, string eventName, int attempts, TimeSpan nextRetryIn, string error)
    {
        Upsert(correlationId, eventName, record =>
        {
            record.State = SalesMessageProcessingState.Retrying.ToString();
            record.Attempts = attempts;
            record.LastError = error;
            record.NextRetryInSeconds = (int)Math.Ceiling(nextRetryIn.TotalSeconds);
            record.UpdatedAtUtc = DateTime.UtcNow;
        });
    }

    public void MarkSucceeded(string correlationId, string eventName, int attempts)
    {
        Upsert(correlationId, eventName, record =>
        {
            record.State = SalesMessageProcessingState.Succeeded.ToString();
            record.Attempts = attempts;
            record.LastError = null;
            record.NextRetryInSeconds = null;
            record.UpdatedAtUtc = DateTime.UtcNow;
        });
    }

    public void MarkDeadLettered(string correlationId, string eventName, int attempts, string error)
    {
        Upsert(correlationId, eventName, record =>
        {
            record.State = SalesMessageProcessingState.DeadLettered.ToString();
            record.Attempts = attempts;
            record.LastError = error;
            record.NextRetryInSeconds = null;
            record.UpdatedAtUtc = DateTime.UtcNow;
        });
    }

    public bool TryGet(string correlationId, out SalesMessageStatus? status)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        var record = dbContext.SalesMessageStatuses
            .AsNoTracking()
            .FirstOrDefault(x => x.CorrelationId == correlationId);

        if (record == null)
        {
            status = null;
            return false;
        }

        status = new SalesMessageStatus
        {
            CorrelationId = record.CorrelationId,
            EventName = record.EventName,
            State = Enum.TryParse<SalesMessageProcessingState>(record.State, out var parsed)
                ? parsed
                : SalesMessageProcessingState.Queued,
            Attempts = record.Attempts,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc,
            LastError = record.LastError,
            NextRetryInSeconds = record.NextRetryInSeconds,
            PayloadJson = record.PayloadJson
        };

        return true;
    }

    private void Upsert(string correlationId, string eventName, Action<SalesMessageStatusRecord> updateAction)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        var record = dbContext.SalesMessageStatuses
            .FirstOrDefault(x => x.CorrelationId == correlationId);

        if (record == null)
        {
            record = new SalesMessageStatusRecord
            {
                CorrelationId = correlationId,
                EventName = eventName,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                State = SalesMessageProcessingState.Queued.ToString()
            };
            dbContext.SalesMessageStatuses.Add(record);
        }

        record.EventName = eventName;
        updateAction(record);
        if (record.CreatedAtUtc == default)
            record.CreatedAtUtc = DateTime.UtcNow;
        if (record.UpdatedAtUtc == default)
            record.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.SaveChanges();
    }
}

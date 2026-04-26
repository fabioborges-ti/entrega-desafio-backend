namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public enum SalesMessageProcessingState
{
    Queued,
    Processing,
    Retrying,
    Succeeded,
    DeadLettered
}

public sealed class SalesMessageStatus
{
    public string CorrelationId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public SalesMessageProcessingState State { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? LastError { get; set; }
    public int? NextRetryInSeconds { get; set; }
}

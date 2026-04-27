namespace Ambev.DeveloperEvaluation.ORM.Messaging;

public sealed class SalesMessageStatusRecord
{
    public string CorrelationId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? LastError { get; set; }
    public int? NextRetryInSeconds { get; set; }
    public string? PayloadJson { get; set; }
}

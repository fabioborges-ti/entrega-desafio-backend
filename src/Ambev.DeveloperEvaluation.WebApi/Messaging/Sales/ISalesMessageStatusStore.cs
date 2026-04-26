namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public interface ISalesMessageStatusStore
{
    void MarkQueued(string correlationId, string eventName);
    void MarkProcessing(string correlationId, string eventName, int attempts);
    void MarkRetrying(string correlationId, string eventName, int attempts, TimeSpan nextRetryIn, string error);
    void MarkSucceeded(string correlationId, string eventName, int attempts);
    void MarkDeadLettered(string correlationId, string eventName, int attempts, string error);
    bool TryGet(string correlationId, out SalesMessageStatus? status);
}

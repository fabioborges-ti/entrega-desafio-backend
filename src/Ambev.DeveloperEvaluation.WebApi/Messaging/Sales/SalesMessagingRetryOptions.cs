namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public sealed class SalesMessagingRetryOptions
{
    public const string SectionName = "SalesMessaging:Retry";

    public int MaxRetries { get; set; } = 3;

    public int[] BackoffSeconds { get; set; } = [2, 5, 15];

    public TimeSpan GetBackoffForAttempt(int attempt)
    {
        if (attempt <= 0)
            return TimeSpan.Zero;

        if (BackoffSeconds.Length == 0)
            return TimeSpan.FromSeconds(2);

        var index = Math.Min(attempt - 1, BackoffSeconds.Length - 1);
        var seconds = Math.Max(1, BackoffSeconds[index]);
        return TimeSpan.FromSeconds(seconds);
    }
}

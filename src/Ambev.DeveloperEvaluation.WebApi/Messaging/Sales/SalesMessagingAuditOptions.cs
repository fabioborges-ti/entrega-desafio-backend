namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public sealed class SalesMessagingAuditOptions
{
    public const string SectionName = "SalesMessaging:Audit";

    public int MaxPayloadLength { get; set; } = 64_000;
}

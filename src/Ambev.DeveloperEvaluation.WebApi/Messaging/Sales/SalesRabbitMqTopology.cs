namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public static class SalesRabbitMqTopology
{
    public const string EventsExchange = "devstore.sales.events.v1";
    public const string DeadLetterExchange = "devstore.sales.dlx.v1";
    public const string DeadLetterQueue = "devstore.sales.dlq.v1";

    public const string CreatedQueue = "devstore.sales.created.persist.v1";
    public const string ModifiedQueue = "devstore.sales.modified.persist.v1";
    public const string CancelledQueue = "devstore.sales.cancelled.persist.v1";
    public const string DeletedQueue = "devstore.sales.deleted.persist.v1";

    public const string CreatedRoutingKey = "sale.created.v1";
    public const string ModifiedRoutingKey = "sale.modified.v1";
    public const string CancelledRoutingKey = "sale.cancelled.v1";
    public const string DeletedRoutingKey = "sale.deleted.v1";
}

using System.Text;
using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Configuration;
using Ambev.DeveloperEvaluation.Common.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Diagnostics;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public sealed class RabbitMqSaleCommandPublisher : ISaleCommandPublisher
{
    private const int DefaultMaxAuditPayloadLength = 64_000;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RabbitMqOptions _options;
    private readonly SalesMessagingAuditOptions _auditOptions;
    private readonly ISalesMessageStatusStore _statusStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RabbitMqSaleCommandPublisher> _logger;

    public RabbitMqSaleCommandPublisher(
        IOptions<RabbitMqOptions> options,
        IOptions<SalesMessagingAuditOptions> auditOptions,
        ISalesMessageStatusStore statusStore,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RabbitMqSaleCommandPublisher> logger)
    {
        _options = options.Value;
        _auditOptions = auditOptions.Value;
        _statusStore = statusStore;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<string> PublishCreateAsync(CreateSaleRequestedMessage payload, CancellationToken cancellationToken) =>
        PublishAsync(SalesRabbitMqTopology.CreatedRoutingKey, payload, cancellationToken);

    public Task<string> PublishUpdateAsync(UpdateSaleRequestedMessage payload, CancellationToken cancellationToken) =>
        PublishAsync(SalesRabbitMqTopology.ModifiedRoutingKey, payload, cancellationToken);

    public Task<string> PublishCancelAsync(CancelSaleRequestedMessage payload, CancellationToken cancellationToken) =>
        PublishAsync(SalesRabbitMqTopology.CancelledRoutingKey, payload, cancellationToken);

    public Task<string> PublishDeleteAsync(DeleteSaleRequestedMessage payload, CancellationToken cancellationToken) =>
        PublishAsync(SalesRabbitMqTopology.DeletedRoutingKey, payload, cancellationToken);

    private async Task<string> PublishAsync<T>(string routingKey, T payload, CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId();
        var currentActivity = Activity.Current;
        var factory = BuildFactory();

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await DeclareTopologyAsync(channel, cancellationToken);

        var envelope = new
        {
            eventName = routingKey,
            correlationId,
            occurredAtUtc = DateTime.UtcNow,
            payload
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, JsonOptions));
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            CorrelationId = correlationId,
            MessageId = correlationId,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };
        properties.Headers = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(currentActivity?.Id))
            properties.Headers["traceparent"] = currentActivity.Id;
        if (!string.IsNullOrWhiteSpace(currentActivity?.TraceStateString))
            properties.Headers["tracestate"] = currentActivity.TraceStateString;

        await channel.BasicPublishAsync(
            exchange: SalesRabbitMqTopology.EventsExchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        var auditPayload = BuildAuditPayload(body, correlationId);
        _statusStore.MarkQueued(correlationId, routingKey, auditPayload);
        _logger.LogInformation(
            "Evento de venda publicado em RabbitMQ. Event: {EventName}, CorrelationId: {CorrelationId}, TraceId: {TraceId}",
            routingKey,
            correlationId,
            currentActivity?.TraceId.ToString() ?? string.Empty);

        return correlationId;
    }

    private string ResolveCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var correlationIdObj) == true &&
            correlationIdObj is string correlationIdFromContext &&
            !string.IsNullOrWhiteSpace(correlationIdFromContext))
        {
            return correlationIdFromContext;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static async Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: SalesRabbitMqTopology.EventsExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: SalesRabbitMqTopology.DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: SalesRabbitMqTopology.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: SalesRabbitMqTopology.DeadLetterQueue,
            exchange: SalesRabbitMqTopology.DeadLetterExchange,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = SalesRabbitMqTopology.DeadLetterExchange
        };

        await channel.QueueDeclareAsync(
            queue: SalesRabbitMqTopology.CreatedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: SalesRabbitMqTopology.ModifiedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: SalesRabbitMqTopology.CancelledQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: SalesRabbitMqTopology.DeletedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: SalesRabbitMqTopology.CreatedQueue,
            exchange: SalesRabbitMqTopology.EventsExchange,
            routingKey: SalesRabbitMqTopology.CreatedRoutingKey,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: SalesRabbitMqTopology.ModifiedQueue,
            exchange: SalesRabbitMqTopology.EventsExchange,
            routingKey: SalesRabbitMqTopology.ModifiedRoutingKey,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: SalesRabbitMqTopology.CancelledQueue,
            exchange: SalesRabbitMqTopology.EventsExchange,
            routingKey: SalesRabbitMqTopology.CancelledRoutingKey,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: SalesRabbitMqTopology.DeletedQueue,
            exchange: SalesRabbitMqTopology.EventsExchange,
            routingKey: SalesRabbitMqTopology.DeletedRoutingKey,
            cancellationToken: cancellationToken);
    }

    private ConnectionFactory BuildFactory() =>
        new()
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };

    private string BuildAuditPayload(byte[] body, string correlationId)
    {
        var maxLength = _auditOptions.MaxPayloadLength > 0
            ? _auditOptions.MaxPayloadLength
            : DefaultMaxAuditPayloadLength;

        var payload = Encoding.UTF8.GetString(body);
        if (payload.Length <= maxLength)
            return payload;

        _logger.LogWarning(
            "Payload de auditoria truncado. CorrelationId: {CorrelationId}, TamanhoOriginal: {OriginalLength}, Limite: {Limit}",
            correlationId,
            payload.Length,
            maxLength);

        return payload[..maxLength];
    }
}

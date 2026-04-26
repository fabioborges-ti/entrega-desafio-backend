using System.Text;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Configuration;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public sealed class SaleCommandConsumerBackgroundService : BackgroundService
{
    private const string RetryCountHeader = "x-retry-count";
    private static readonly ActivitySource ActivitySource = new("Ambev.DeveloperEvaluation.WebApi.SalesMessaging");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RabbitMqOptions _options;
    private readonly SalesMessagingRetryOptions _retryOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SaleCommandConsumerBackgroundService> _logger;
    private readonly ISalesMessageStatusStore _statusStore;

    public SaleCommandConsumerBackgroundService(
        IOptions<RabbitMqOptions> options,
        IOptions<SalesMessagingRetryOptions> retryOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<SaleCommandConsumerBackgroundService> logger,
        ISalesMessageStatusStore statusStore)
    {
        _options = options.Value;
        _retryOptions = retryOptions.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _statusStore = statusStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = BuildFactory();
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await DeclareTopologyAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        await ConsumeQueueAsync(channel, SalesRabbitMqTopology.CreatedQueue, stoppingToken);
        await ConsumeQueueAsync(channel, SalesRabbitMqTopology.ModifiedQueue, stoppingToken);
        await ConsumeQueueAsync(channel, SalesRabbitMqTopology.CancelledQueue, stoppingToken);
        await ConsumeQueueAsync(channel, SalesRabbitMqTopology.DeletedQueue, stoppingToken);

        _logger.LogInformation("RabbitMQ consumer de vendas iniciado.");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQ consumer de vendas finalizado.");
        }
    }

    private async Task ConsumeQueueAsync(IChannel channel, string queue, CancellationToken cancellationToken)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            await ProcessMessageAsync(channel, queue, args, cancellationToken);
        };

        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    private async Task ProcessMessageAsync(
        IChannel channel,
        string queue,
        BasicDeliverEventArgs args,
        CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetString(args.Body.ToArray());
        var correlationId = args.BasicProperties?.MessageId ?? Guid.NewGuid().ToString("N");
        var retryCount = GetRetryCount(args.BasicProperties);
        var attempt = retryCount + 1;
        var eventName = args.RoutingKey;
        var parentContext = ResolveParentActivityContext(args.BasicProperties);
        using var activity = ActivitySource.StartActivity(
            $"sales.consume.{eventName}",
            ActivityKind.Consumer,
            parentContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", queue);
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.message.id", correlationId);
        activity?.SetTag("messaging.rabbitmq.routing_key", eventName);
        activity?.SetTag("messaging.retry.attempt", attempt);

        try
        {
            var envelope = JsonSerializer.Deserialize<SaleMessageEnvelope>(body, JsonOptions);
            if (envelope == null)
                throw new InvalidOperationException("Envelope de mensagem inválido.");

            correlationId = string.IsNullOrWhiteSpace(envelope.CorrelationId)
                ? correlationId
                : envelope.CorrelationId;
            eventName = string.IsNullOrWhiteSpace(envelope.EventName)
                ? eventName
                : envelope.EventName;

            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = correlationId,
                ["SaleMessagingEvent"] = eventName,
                ["Queue"] = queue,
                ["MessagingSource"] = "RabbitMq",
                ["TraceId"] = activity?.TraceId.ToString() ?? string.Empty,
                ["SpanId"] = activity?.SpanId.ToString() ?? string.Empty
            }))
            {
                _statusStore.MarkProcessing(correlationId, eventName, attempt);
                await DispatchAsync(envelope, cancellationToken);
                _statusStore.MarkSucceeded(correlationId, eventName, attempt);
                _logger.LogInformation(
                    "Mensagem de venda processada com sucesso. Event: {EventName}, CorrelationId: {CorrelationId}, Attempt: {Attempt}",
                    eventName,
                    correlationId,
                    attempt);
            }

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar mensagem da fila {Queue}. Payload: {Payload}", queue, body);

            if (attempt <= _retryOptions.MaxRetries)
            {
                var backoff = _retryOptions.GetBackoffForAttempt(attempt);
                _statusStore.MarkRetrying(correlationId, eventName, attempt, backoff, ex.Message);
                _logger.LogWarning(
                    "Falha transitória no processamento de venda. Event: {EventName}, CorrelationId: {CorrelationId}, Attempt: {Attempt}, RetryInSeconds: {RetryInSeconds}",
                    eventName,
                    correlationId,
                    attempt,
                    backoff.TotalSeconds);

                await Task.Delay(backoff, cancellationToken);
                await RepublishForRetryAsync(channel, args, body, correlationId, attempt, cancellationToken);
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken);
                return;
            }

            _statusStore.MarkDeadLettered(correlationId, eventName, attempt, ex.Message);
            _logger.LogError(
                "Mensagem enviada para DLQ. Event: {EventName}, CorrelationId: {CorrelationId}, Attempts: {Attempts}",
                eventName,
                correlationId,
                attempt);
            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken);
        }
    }

    private async Task DispatchAsync(SaleMessageEnvelope envelope, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        switch (envelope.EventName)
        {
            case SalesRabbitMqTopology.CreatedRoutingKey:
            {
                var payload = envelope.Payload.Deserialize<CreateSaleRequestedMessage>(JsonOptions)
                    ?? throw new InvalidOperationException("Payload de criação de venda inválido.");

                await mediator.Send(new CreateSaleCommand
                {
                    SaleDate = payload.SaleDate,
                    SaleNumber = payload.SaleNumber,
                    CustomerId = payload.CustomerId,
                    BranchId = payload.BranchId,
                    CartId = payload.CartId,
                    SuppressEventPublication = true
                }, cancellationToken);
                break;
            }
            case SalesRabbitMqTopology.ModifiedRoutingKey:
            {
                var payload = envelope.Payload.Deserialize<UpdateSaleRequestedMessage>(JsonOptions)
                    ?? throw new InvalidOperationException("Payload de atualização de venda inválido.");

                await mediator.Send(new UpdateSaleCommand
                {
                    Id = payload.Id,
                    SaleDate = payload.SaleDate,
                    CustomerId = payload.CustomerId,
                    BranchId = payload.BranchId,
                    CartId = payload.CartId,
                    SuppressEventPublication = true
                }, cancellationToken);
                break;
            }
            case SalesRabbitMqTopology.CancelledRoutingKey:
            {
                var payload = envelope.Payload.Deserialize<CancelSaleRequestedMessage>(JsonOptions)
                    ?? throw new InvalidOperationException("Payload de cancelamento de venda inválido.");

                await mediator.Send(new CancelSaleCommand(payload.Id)
                {
                    SuppressEventPublication = true
                }, cancellationToken);
                break;
            }
            case SalesRabbitMqTopology.DeletedRoutingKey:
            {
                var payload = envelope.Payload.Deserialize<DeleteSaleRequestedMessage>(JsonOptions)
                    ?? throw new InvalidOperationException("Payload de exclusão de venda inválido.");

                await mediator.Send(new DeleteSaleCommand(payload.Id), cancellationToken);
                break;
            }
            default:
                throw new InvalidOperationException($"Evento de venda desconhecido: {envelope.EventName}");
        }
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

    private static int GetRetryCount(IReadOnlyBasicProperties? properties)
    {
        if (properties?.Headers == null)
            return 0;

        if (!properties.Headers.TryGetValue(RetryCountHeader, out var value) || value == null)
            return 0;

        return value switch
        {
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            sbyte s => s,
            byte b => b,
            short s => s,
            ushort u => u,
            int i => i,
            long l => (int)l,
            string str when int.TryParse(str, out var parsed) => parsed,
            _ => 0
        };
    }

    private static async Task RepublishForRetryAsync(
        IChannel channel,
        BasicDeliverEventArgs args,
        string body,
        string correlationId,
        int retryCount,
        CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, object?>();
        if (args.BasicProperties?.Headers != null)
        {
            foreach (var pair in args.BasicProperties.Headers)
            {
                headers[pair.Key] = pair.Value;
            }
        }

        headers[RetryCountHeader] = retryCount.ToString();

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = correlationId,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Headers = headers
        };

        await channel.BasicPublishAsync(
            exchange: SalesRabbitMqTopology.EventsExchange,
            routingKey: args.RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(body),
            cancellationToken: cancellationToken);
    }

    private static ActivityContext ResolveParentActivityContext(IReadOnlyBasicProperties? properties)
    {
        if (properties?.Headers == null)
            return default;

        properties.Headers.TryGetValue("traceparent", out var traceParentValue);
        properties.Headers.TryGetValue("tracestate", out var traceStateValue);

        var traceParent = ReadHeaderAsString(traceParentValue);
        if (string.IsNullOrWhiteSpace(traceParent))
            return default;

        var traceState = ReadHeaderAsString(traceStateValue);
        return ActivityContext.TryParse(traceParent, traceState, out var context)
            ? context
            : default;
    }

    private static string? ReadHeaderAsString(object? value) => value switch
    {
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        string text => text,
        _ => null
    };
}


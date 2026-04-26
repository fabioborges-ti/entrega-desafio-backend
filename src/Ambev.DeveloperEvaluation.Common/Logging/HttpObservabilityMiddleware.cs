using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Ambev.DeveloperEvaluation.Common.Logging;

/// <summary>
/// Captura métricas de requisição HTTP e registra eventos estruturados para investigação.
/// </summary>
public sealed class HttpObservabilityMiddleware
{
    private static readonly Meter Meter = new("Ambev.DeveloperEvaluation.WebApi");
    private static readonly Counter<long> RequestsTotal = Meter.CreateCounter<long>(
        "http.server.requests.total",
        unit: "requests",
        description: "Total de requisições HTTP processadas.");
    private static readonly Histogram<double> RequestDurationMs = Meter.CreateHistogram<double>(
        "http.server.request.duration",
        unit: "ms",
        description: "Latência de requisições HTTP.");

    private readonly RequestDelegate _next;
    private readonly ILogger<HttpObservabilityMiddleware> _logger;

    public HttpObservabilityMiddleware(
        RequestDelegate next,
        ILogger<HttpObservabilityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        var endpoint = context.GetEndpoint() as RouteEndpoint;
        var route = endpoint?.RoutePattern.RawText ?? "unknown";
        var method = context.Request.Method;
        var statusCode = context.Response.StatusCode;
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var value)
            ? value?.ToString() ?? string.Empty
            : string.Empty;
        var activity = Activity.Current;
        var traceId = activity?.TraceId.ToString() ?? string.Empty;
        var spanId = activity?.SpanId.ToString() ?? string.Empty;

        var tags = new TagList
        {
            { "http.method", method },
            { "http.route", route },
            { "http.status_code", statusCode },
            { "correlation.id", correlationId }
        };

        RequestsTotal.Add(1, tags);
        RequestDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, tags);

        _logger.LogInformation(
            "HTTP request concluída: {Method} {Path} -> {StatusCode} em {ElapsedMs}ms (CorrelationId: {CorrelationId}, TraceId: {TraceId}, SpanId: {SpanId})",
            method,
            context.Request.Path.Value,
            statusCode,
            stopwatch.Elapsed.TotalMilliseconds,
            correlationId,
            traceId,
            spanId);
    }
}

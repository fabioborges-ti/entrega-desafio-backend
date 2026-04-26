using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace Ambev.DeveloperEvaluation.Common.Logging;

/// <summary>
/// Propaga <c>X-Correlation-Id</c> (ou gera um), replica no cabeçalho de resposta e enriquece o Serilog <see cref="LogContext"/>.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string HttpContextItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        context.Items[HttpContextItemKey] = correlationId;
        context.Response.Headers.Append(HeaderName, correlationId);
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await _next(context);
    }
}

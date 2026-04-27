using Ambev.DeveloperEvaluation.Common.Logging;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Logging;

public class ObservabilityMiddlewaresTests
{
    [Fact(DisplayName = "CorrelationIdMiddleware gera correlation id quando header não existe")]
    public async Task CorrelationIdMiddleware_GeneratesId_WhenHeaderMissing()
    {
        var httpContext = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(httpContext);

        httpContext.Items.Should().ContainKey(CorrelationIdMiddleware.HttpContextItemKey);
        var correlationId = httpContext.Items[CorrelationIdMiddleware.HttpContextItemKey]?.ToString();
        correlationId.Should().NotBeNullOrWhiteSpace();
        correlationId!.Length.Should().Be(32);
        httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be(correlationId);
    }

    [Fact(DisplayName = "CorrelationIdMiddleware reutiliza header de correlação recebido")]
    public async Task CorrelationIdMiddleware_UsesIncomingHeader()
    {
        var expectedCorrelationId = "corr-abc-123";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[CorrelationIdMiddleware.HeaderName] = expectedCorrelationId;

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(httpContext);

        httpContext.Items[CorrelationIdMiddleware.HttpContextItemKey].Should().Be(expectedCorrelationId);
        httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be(expectedCorrelationId);
    }

    [Fact(DisplayName = "HttpObservabilityMiddleware mantém status e registra dados da requisição")]
    public async Task HttpObservabilityMiddleware_PreservesStatus_AndReadsRouteAndCorrelation()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/sales";
        httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
        httpContext.Items[CorrelationIdMiddleware.HttpContextItemKey] = "corr-observability";

        var endpoint = new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/sales"),
            0,
            EndpointMetadataCollection.Empty,
            "sales-endpoint");
        httpContext.SetEndpoint(endpoint);

        var middleware = new HttpObservabilityMiddleware(
            _ => Task.CompletedTask,
            NullLogger<HttpObservabilityMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }
}

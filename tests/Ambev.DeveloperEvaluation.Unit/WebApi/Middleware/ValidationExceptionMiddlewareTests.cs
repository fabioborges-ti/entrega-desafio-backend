using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Middleware;

public class ValidationExceptionMiddlewareTests
{
    private static async Task<(int StatusCode, string Body, string ContentType)> InvokeAsync(RequestDelegate next)
    {
        var middleware = new ValidationExceptionMiddleware(next, NullLogger<ValidationExceptionMiddleware>.Instance);
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(ctx);

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(ctx.Response.Body);
        var body = await reader.ReadToEndAsync();
        return (ctx.Response.StatusCode, body, ctx.Response.ContentType ?? string.Empty);
    }

    [Fact(DisplayName = "Quando next executa sem exceção, middleware não altera o status")]
    public async Task NoException_DoesNotChangeStatus()
    {
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var (status, body, _) = await InvokeAsync(next);

        status.Should().Be(200);
        body.Should().BeEmpty();
    }

    [Fact(DisplayName = "Captura ValidationException e retorna 400 com payload de erro")]
    public async Task ValidationException_Returns400WithJsonPayload()
    {
        var failures = new[]
        {
            new ValidationFailure("Field1", "is required"),
            new ValidationFailure("Field2", "must be greater than 0")
        };

        RequestDelegate next = _ => throw new ValidationException(failures);

        var (status, body, contentType) = await InvokeAsync(next);

        status.Should().Be(StatusCodes.Status400BadRequest);
        contentType.Should().Be("application/json");

        var response = JsonSerializer.Deserialize<ApiErrorResponse>(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        response.Should().NotBeNull();
        response!.Type.Should().Be("ValidationError");
        response.Error.Should().Be("Invalid input data");
        response.Detail.Should().Contain("Field1: is required");
        response.Detail.Should().Contain("Field2: must be greater than 0");
    }

    [Fact(DisplayName = "Outras exceções não são capturadas pelo middleware")]
    public async Task OtherException_IsNotCaught()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("boom");
        var middleware = new ValidationExceptionMiddleware(next, NullLogger<ValidationExceptionMiddleware>.Instance);
        var ctx = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        var act = async () => await middleware.InvokeAsync(ctx);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}


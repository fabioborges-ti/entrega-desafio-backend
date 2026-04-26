using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Common.Logging;

/// <summary>
/// Registra duração e resultado de cada request MediatR (inclui validação em pipeline).
/// </summary>
public sealed class MediatRLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger _logger;

    public MediatRLoggingBehavior(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("MediatR");
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        var requestScopeValues = BuildRequestScopeValues(requestName, request);

        using (_logger.BeginScope(requestScopeValues))
        {
            _logger.LogDebug("MediatR iniciado: {MediatRRequest}", requestName);

            try
            {
                var response = await next();
                sw.Stop();
                _logger.LogInformation(
                    "MediatR concluído: {MediatRRequest} em {ElapsedMilliseconds} ms",
                    requestName,
                    sw.ElapsedMilliseconds);
                return response;
            }
            catch (ValidationException ex)
            {
                sw.Stop();
                var errorCount = ex.Errors.Count();
                _logger.LogWarning(
                    "MediatR falhou (validação): {MediatRRequest} após {ElapsedMilliseconds} ms - {ValidationErrorCount} erro(s)",
                    requestName,
                    sw.ElapsedMilliseconds,
                    errorCount);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(
                    ex,
                    "MediatR falhou: {MediatRRequest} após {ElapsedMilliseconds} ms",
                    requestName,
                    sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private static Dictionary<string, object?> BuildRequestScopeValues(string requestName, TRequest request)
    {
        var scopeValues = new Dictionary<string, object?>
        {
            ["MediatRRequest"] = requestName
        };

        var properties = request.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .Where(p => IsRelevantProperty(p.Name));

        foreach (var property in properties)
        {
            var value = property.GetValue(request);
            if (value == null)
                continue;

            if (!IsSafeScalar(value))
                continue;

            scopeValues[property.Name] = value;
        }

        return scopeValues;
    }

    private static bool IsRelevantProperty(string propertyName) =>
        propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Equals("Page", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Equals("PageSize", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Equals("Search", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Equals("Sort", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Equals("OrderBy", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Equals("Category", StringComparison.OrdinalIgnoreCase);

    private static bool IsSafeScalar(object value)
    {
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum)
            return true;

        if (value is string || value is Guid || value is DateTime || value is DateTimeOffset || value is decimal)
            return true;

        return value is not IEnumerable;
    }
}

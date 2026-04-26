using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Common.Validation;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILoggerFactory loggerFactory)
    {
        _validators = validators;
        _logger = loggerFactory.CreateLogger("FluentValidation.MediatR");
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                var propertyNames = string.Join(',', failures.Select(f => f.PropertyName).Where(n => !string.IsNullOrEmpty(n)).Distinct());
                _logger.LogWarning(
                    "Validação rejeitada para {RequestType}: {FailureCount} erro(s). Propriedades: {PropertyNames}",
                    typeof(TRequest).Name,
                    failures.Count,
                    string.IsNullOrEmpty(propertyNames) ? "(n/d)" : propertyNames);
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
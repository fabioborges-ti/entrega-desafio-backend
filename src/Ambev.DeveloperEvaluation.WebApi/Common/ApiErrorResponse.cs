using FluentValidation.Results;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>Corpo de erro conforme <c>general-api.md</c> (type, error, detail).</summary>
public sealed class ApiErrorResponse
{
    public required string Type { get; init; }

    public required string Error { get; init; }

    public required string Detail { get; init; }

    public static ApiErrorResponse FromValidationFailures(IEnumerable<ValidationFailure> failures)
    {
        var parts = failures
            .Select(f => $"{f.PropertyName}: {f.ErrorMessage}".Trim())
            .Where(s => s.Length > 0)
            .ToList();
        var detail = parts.Count > 0 ? string.Join(' ', parts) : "Invalid input data.";
        return new ApiErrorResponse
        {
            Type = "ValidationError",
            Error = "Invalid input data",
            Detail = detail
        };
    }

    public static ApiErrorResponse ValidationDetail(string detail) =>
        new()
        {
            Type = "ValidationError",
            Error = "Invalid input data",
            Detail = detail
        };

    public static ApiErrorResponse ResourceNotFound(string error, string detail) =>
        new()
        {
            Type = "ResourceNotFound",
            Error = error,
            Detail = detail
        };

    public static ApiErrorResponse Authentication(string error, string detail) =>
        new()
        {
            Type = "AuthenticationError",
            Error = error,
            Detail = detail
        };
}

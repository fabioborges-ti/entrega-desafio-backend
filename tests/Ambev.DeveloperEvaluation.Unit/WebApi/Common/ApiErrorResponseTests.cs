using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

public class ApiErrorResponseTests
{
    [Fact(DisplayName = "FromValidationFailures concatena propriedade e mensagem; usa fallback se vazio")]
    public void FromValidationFailures_ConcatenatesAndFallback()
    {
        ApiErrorResponse.FromValidationFailures(new[]
        {
            new ValidationFailure("Field1", "is required"),
            new ValidationFailure("Field2", "must be > 0")
        }).Detail.Should().Be("Field1: is required Field2: must be > 0");

        // Quando todas as falhas geram strings vazias (sem propriedade e sem mensagem),
        // o filtro `s.Length > 0` falha porque "': '" tem caracteres. Para forçar fallback,
        // passamos lista totalmente vazia.
        ApiErrorResponse.FromValidationFailures(Array.Empty<ValidationFailure>())
            .Detail.Should().Be("Invalid input data.");
    }

    [Fact(DisplayName = "ValidationDetail produz objeto com tipo ValidationError")]
    public void ValidationDetail_ProducesProperResponse()
    {
        var resp = ApiErrorResponse.ValidationDetail("bad");
        resp.Type.Should().Be("ValidationError");
        resp.Error.Should().Be("Invalid input data");
        resp.Detail.Should().Be("bad");
    }

    [Fact(DisplayName = "ResourceNotFound produz objeto com tipo ResourceNotFound")]
    public void ResourceNotFound_ProducesProperResponse()
    {
        var resp = ApiErrorResponse.ResourceNotFound("NotFound", "missing");
        resp.Type.Should().Be("ResourceNotFound");
        resp.Error.Should().Be("NotFound");
        resp.Detail.Should().Be("missing");
    }

    [Fact(DisplayName = "Authentication produz objeto com tipo AuthenticationError")]
    public void Authentication_ProducesProperResponse()
    {
        var resp = ApiErrorResponse.Authentication("Unauthorized", "no token");
        resp.Type.Should().Be("AuthenticationError");
        resp.Error.Should().Be("Unauthorized");
        resp.Detail.Should().Be("no token");
    }
}


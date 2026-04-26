using Ambev.DeveloperEvaluation.Common.Validation;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Validation;

public class ValidationErrorDetailTests
{
    [Fact(DisplayName = "Conversão explícita de ValidationFailure preserva código e mensagem")]
    public void ExplicitOperator_FromValidationFailure_PreservesErrorCodeAndMessage()
    {
        var failure = new ValidationFailure("Property", "Mensagem de erro")
        {
            ErrorCode = "Required"
        };

        var detail = (ValidationErrorDetail)failure;

        detail.Detail.Should().Be("Mensagem de erro");
        detail.Error.Should().Be("Required");
    }

    [Fact(DisplayName = "ValidationResultDetail criado a partir de ValidationResult preserva validade e erros")]
    public void Constructor_FromValidationResult_PreservesValidAndErrors()
    {
        var failure = new ValidationFailure("Email", "Email inválido") { ErrorCode = "EMAIL" };
        var result = new ValidationResult(new[] { failure });

        var detail = new ValidationResultDetail(result);

        detail.IsValid.Should().BeFalse();
        detail.Errors.Should().HaveCount(1);
        detail.Errors.First().Detail.Should().Be("Email inválido");
        detail.Errors.First().Error.Should().Be("EMAIL");
    }

    [Fact(DisplayName = "Construtor padrão de ValidationResultDetail inicializa IsValid=false e sem erros")]
    public void Constructor_Default_LeavesEmptyErrorsAndInvalid()
    {
        var detail = new ValidationResultDetail();

        detail.Errors.Should().BeEmpty();
        detail.IsValid.Should().BeFalse();
    }
}


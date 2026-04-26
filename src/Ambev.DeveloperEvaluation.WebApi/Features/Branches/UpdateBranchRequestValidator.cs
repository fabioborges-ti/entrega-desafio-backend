using Ambev.DeveloperEvaluation.Application.Branches;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches;

public class UpdateBranchRequestValidator : AbstractValidator<UpdateBranchRequest>
{
    public UpdateBranchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cnpj).NotEmpty();
        RuleFor(x => x.Cnpj)
            .Must(c => CnpjDigits.HasValidLength(CnpjDigits.Normalize(c)))
            .WithMessage("CNPJ deve conter 14 dígitos.");
    }
}


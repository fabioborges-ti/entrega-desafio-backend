using Ambev.DeveloperEvaluation.Application.Branches;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;

public class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cnpj).NotEmpty();
        RuleFor(x => x.Cnpj)
            .Must(c => CnpjDigits.HasValidLength(CnpjDigits.Normalize(c)))
            .WithMessage("CNPJ deve conter 14 dígitos.");
    }
}


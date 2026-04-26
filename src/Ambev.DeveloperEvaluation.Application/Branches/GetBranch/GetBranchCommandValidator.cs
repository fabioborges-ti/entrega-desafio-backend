using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Branches.GetBranch;

public class GetBranchCommandValidator : AbstractValidator<GetBranchCommand>
{
    public GetBranchCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

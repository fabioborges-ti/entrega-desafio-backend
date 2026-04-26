using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Branches.ListBranches;

public class ListBranchesCommandValidator : AbstractValidator<ListBranchesCommand>
{
    public ListBranchesCommandValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}

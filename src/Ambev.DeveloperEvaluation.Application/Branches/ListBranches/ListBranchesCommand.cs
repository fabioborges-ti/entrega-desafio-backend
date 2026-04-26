using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.ListBranches;

public class ListBranchesCommand : IRequest<ListBranchesResult>
{
    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;
}

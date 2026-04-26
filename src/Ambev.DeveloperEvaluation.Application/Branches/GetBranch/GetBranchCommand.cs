using Ambev.DeveloperEvaluation.Application.Branches;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.GetBranch;

public class GetBranchCommand : IRequest<BranchDto>
{
    public int Id { get; set; }
}


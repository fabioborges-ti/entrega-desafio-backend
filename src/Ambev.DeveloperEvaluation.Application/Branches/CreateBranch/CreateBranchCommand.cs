using Ambev.DeveloperEvaluation.Application.Branches;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;

public class CreateBranchCommand : IRequest<BranchDto>
{
    public string Name { get; set; } = string.Empty;

    public string Cnpj { get; set; } = string.Empty;

    public int CreatedByUserId { get; set; }
}

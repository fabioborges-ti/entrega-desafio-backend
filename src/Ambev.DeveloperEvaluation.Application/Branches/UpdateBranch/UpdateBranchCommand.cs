using Ambev.DeveloperEvaluation.Application.Branches;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;

public class UpdateBranchCommand : IRequest<BranchDto>
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Cnpj { get; set; } = string.Empty;
}


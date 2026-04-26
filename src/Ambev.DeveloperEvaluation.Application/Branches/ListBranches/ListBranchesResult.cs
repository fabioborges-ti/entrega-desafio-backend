using Ambev.DeveloperEvaluation.Application.Branches;

namespace Ambev.DeveloperEvaluation.Application.Branches.ListBranches;

public class ListBranchesResult
{
    public IReadOnlyList<BranchDto> Data { get; set; } = Array.Empty<BranchDto>();

    public int TotalItems { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }
}

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches;

public class ListBranchesResponse
{
    public IReadOnlyList<BranchResponse> Data { get; set; } = Array.Empty<BranchResponse>();

    public int TotalItems { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }
}

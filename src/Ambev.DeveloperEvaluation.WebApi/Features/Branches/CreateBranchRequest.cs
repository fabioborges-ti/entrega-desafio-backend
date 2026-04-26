namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches;

public class CreateBranchRequest
{
    public string Name { get; set; } = string.Empty;

    public string Cnpj { get; set; } = string.Empty;
}

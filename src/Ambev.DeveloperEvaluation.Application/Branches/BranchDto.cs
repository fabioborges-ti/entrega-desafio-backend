namespace Ambev.DeveloperEvaluation.Application.Branches;

public class BranchDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Cnpj { get; set; } = string.Empty;

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }
}


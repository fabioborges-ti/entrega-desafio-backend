using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>CNPJ somente dígitos (14 caracteres).</summary>
    public string Cnpj { get; set; } = string.Empty;

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public User? CreatedByUser { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}


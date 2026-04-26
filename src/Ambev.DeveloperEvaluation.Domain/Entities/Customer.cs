using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}

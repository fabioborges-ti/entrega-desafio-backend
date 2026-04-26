namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// Referência desnormalizada a uma entidade de outro domínio (External Identity).
/// </summary>
public class ExternalIdentity
{
    public Guid ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;
}


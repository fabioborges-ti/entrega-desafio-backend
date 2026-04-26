namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// Nome da pessoa (firstname / lastname na API de usuários).
/// </summary>
public class UserPersonName
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}


using Ambev.DeveloperEvaluation.Application.Users;
using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.Application.Users.GetUser;

/// <summary>
/// Resultado alinhado ao contrato users-api.md.
/// </summary>
public class GetUserResult
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    /// <summary>Hash da senha no domínio; não é exposto nas respostas HTTP de leitura.</summary>
    public string Password { get; set; } = string.Empty;

    public UserPersonNameDto Name { get; set; } = new();

    public UserAddressDto Address { get; set; } = new();

    public string Phone { get; set; } = string.Empty;

    public UserStatus Status { get; set; }

    public UserRole Role { get; set; }
}


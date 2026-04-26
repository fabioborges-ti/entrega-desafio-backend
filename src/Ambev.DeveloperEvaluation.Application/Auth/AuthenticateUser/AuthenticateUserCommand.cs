using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;

/// <summary>
/// Command for authenticating a user in the system.
/// Implements IRequest for mediator pattern handling.
/// </summary>
public class AuthenticateUserCommand : IRequest<AuthenticateUserResult>
{
    /// <summary>
    /// Nome de usuário para autenticação (contrato auth-api.md).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authentication.
    /// Will be verified against the stored hashed password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}


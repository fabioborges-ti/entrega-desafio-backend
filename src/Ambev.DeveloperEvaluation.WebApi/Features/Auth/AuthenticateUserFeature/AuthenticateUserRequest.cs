namespace Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;

/// <summary>
/// Represents the authentication request model for user login.
/// </summary>
public class AuthenticateUserRequest
{
    /// <summary>
    /// Nome de usuário (contrato <c>auth-api.md</c>: campo <c>username</c>).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's password for authentication.
    /// Must match the stored password after hashing.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

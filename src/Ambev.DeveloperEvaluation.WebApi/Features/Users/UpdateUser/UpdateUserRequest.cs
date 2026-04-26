using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.WebApi.Features.Users;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.UpdateUser;

/// <summary>Atualização de usuário; senha opcional (omitir ou vazio mantém a atual).</summary>
public sealed class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? Password { get; set; }

    public UserNameContract Name { get; set; } = new();

    public UserAddressContract Address { get; set; } = new();

    public string Phone { get; set; } = string.Empty;

    public UserStatus Status { get; set; }

    public UserRole Role { get; set; }
}


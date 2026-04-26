using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.UpdateUser;

public sealed class UpdateUserCommand : IRequest<GetUserResult>
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public UserPersonName Name { get; set; } = new();

    public UserAddress Address { get; set; } = new();

    /// <summary>Se nulo ou vazio, a senha atual é mantida.</summary>
    public string? Password { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserStatus Status { get; set; }

    public UserRole Role { get; set; }
}


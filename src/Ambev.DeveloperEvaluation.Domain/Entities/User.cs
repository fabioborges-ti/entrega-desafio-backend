using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Validation;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Usuário conforme <see href="https://github.com/coodesh/mouts-backend-challenge/blob/main/.doc/users-api.md">users-api.md</see>:
/// composição de <see cref="UserPersonName"/> e <see cref="UserAddress"/> (com <see cref="AddressGeolocation"/>).
/// </summary>
public class User : IUser
{
    /// <summary>Identificador numérico (integer na API).</summary>
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public UserPersonName Name { get; set; } = new();

    public UserAddress Address { get; set; } = new();

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public UserStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public ICollection<ProductUserRating> ProductUserRatings { get; set; } = new List<ProductUserRating>();

    string IUser.Id => Id.ToString();

    string IUser.Username => Username;

    string IUser.Role => Role.ToString();

    string IUser.Email => Email;

    string IUser.Status => Status.ToString();

    public User()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public ValidationResultDetail Validate()
    {
        var validator = new UserValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }
}


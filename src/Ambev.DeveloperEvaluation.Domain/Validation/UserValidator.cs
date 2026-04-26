using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(user => user.Email).SetValidator(new EmailValidator());

        RuleFor(user => user.Username)
            .NotEmpty()
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(50).WithMessage("Username cannot be longer than 50 characters.");

        RuleFor(user => user.Name).NotNull().SetValidator(new UserPersonNameValidator());

        RuleFor(user => user.Address).NotNull().SetValidator(new UserAddressValidator());

        RuleFor(user => user.Password).SetValidator(new PasswordValidator());

        RuleFor(user => user.Phone)
            .NotEmpty()
            .MaximumLength(40);

        RuleFor(user => user.Status)
            .NotEqual(UserStatus.Unknown)
            .WithMessage("User status cannot be Unknown.");

        RuleFor(user => user.Role)
            .NotEqual(UserRole.None)
            .WithMessage("User role cannot be None.");
    }
}

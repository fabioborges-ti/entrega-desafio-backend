using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Validation;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Users.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(user => user.Email).SetValidator(new EmailValidator());
        RuleFor(user => user.Username).NotEmpty().Length(3, 50);
        RuleFor(user => user.Name).NotNull().SetValidator(new UserPersonNameValidator());
        RuleFor(user => user.Address).NotNull().SetValidator(new UserAddressValidator());
        RuleFor(user => user.Phone).NotEmpty().MaximumLength(40);
        RuleFor(user => user.Status).NotEqual(UserStatus.Unknown);
        RuleFor(user => user.Role).NotEqual(UserRole.None);

        When(
            x => !string.IsNullOrWhiteSpace(x.Password),
            () => RuleFor(x => x.Password!).SetValidator(new PasswordValidator()));
    }
}

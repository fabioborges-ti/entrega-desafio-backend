using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Validation;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.UpdateUser;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(user => user.Email).SetValidator(new EmailValidator());
        RuleFor(user => user.Username).NotEmpty().Length(3, 50);
        RuleFor(user => user.Name).NotNull();
        RuleFor(user => user.Name.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(user => user.Name.LastName).NotEmpty().MaximumLength(100);
        RuleFor(user => user.Address).NotNull();
        RuleFor(user => user.Address.City).NotEmpty().MaximumLength(100);
        RuleFor(user => user.Address.Street).NotEmpty().MaximumLength(200);
        RuleFor(user => user.Address.Number).GreaterThanOrEqualTo(0);
        RuleFor(user => user.Address.Zipcode).NotEmpty().MaximumLength(20);
        RuleFor(user => user.Address.Geolocation).NotNull();
        RuleFor(user => user.Address.Geolocation.Lat).NotEmpty().MaximumLength(50);
        RuleFor(user => user.Address.Geolocation.Long).NotEmpty().MaximumLength(50);
        RuleFor(user => user.Phone).NotEmpty().MaximumLength(40);
        RuleFor(user => user.Status).NotEqual(UserStatus.Unknown);
        RuleFor(user => user.Role).NotEqual(UserRole.None);

        When(
            x => !string.IsNullOrWhiteSpace(x.Password),
            () => RuleFor(x => x.Password!).SetValidator(new PasswordValidator()));
    }
}

using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

public class UserAddressValidator : AbstractValidator<UserAddress>
{
    public UserAddressValidator()
    {
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Number).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Zipcode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Geolocation).NotNull().SetValidator(new AddressGeolocationValidator());
    }
}

using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

public class AddressGeolocationValidator : AbstractValidator<AddressGeolocation>
{
    public AddressGeolocationValidator()
    {
        RuleFor(x => x.Lat).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Long).NotEmpty().MaximumLength(50);
    }
}

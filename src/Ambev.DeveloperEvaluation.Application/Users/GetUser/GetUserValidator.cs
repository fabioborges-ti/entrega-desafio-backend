using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Users.GetUser;

/// <summary>
/// Validator for GetUserCommand
/// </summary>
public class GetUserValidator : AbstractValidator<GetUserCommand>
{
    /// <summary>
    /// Initializes validation rules for GetUserCommand
    /// </summary>
    public GetUserValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("User ID is required");
    }
}

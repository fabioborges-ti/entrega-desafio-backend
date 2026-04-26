using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

public class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator()
    {
        RuleFor(password => password)
            .NotEmpty()
            .WithMessage("Informe uma senha.")
            .MinimumLength(8)
            .WithMessage("A senha deve ter pelo menos 8 caracteres, incluindo letras maiúsculas e minúsculas, números e um caractere especial.")
            .Matches(@"[A-Z]")
            .WithMessage("A senha deve incluir pelo menos uma letra maiúscula.")
            .Matches(@"[a-z]")
            .WithMessage("A senha deve incluir pelo menos uma letra minúscula.")
            .Matches(@"[0-9]")
            .WithMessage("A senha deve incluir pelo menos um número.")
            .Matches(@"[^a-zA-Z0-9]")
            .WithMessage("A senha deve incluir pelo menos um caractere especial (por exemplo: @, #, $, !, -).");
    }
}
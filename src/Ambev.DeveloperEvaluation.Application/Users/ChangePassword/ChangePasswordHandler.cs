using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.ChangePassword;

public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var validator = new ChangePasswordCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var user = await _userRepository.GetTrackedByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.Password))
            throw new UnauthorizedAccessException("A senha atual informada está incorreta.");

        user.Password = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        var entityValidation = user.Validate();
        if (!entityValidation.IsValid)
        {
            var failures = entityValidation.Errors
                .Select(e => new ValidationFailure(nameof(User), string.IsNullOrEmpty(e.Detail) ? e.Error : e.Detail));
            throw new ValidationException(failures);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        return Unit.Value;
    }
}

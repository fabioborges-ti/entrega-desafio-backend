using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.UpdateUser;

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, GetUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;

    public UpdateUserHandler(IUserRepository userRepository, IMapper mapper, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
    }

    public async Task<GetUserResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var validator = new UpdateUserCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var user = await _userRepository.GetTrackedByIdAsync(request.Id, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {request.Id} not found");

        var emailTaken = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (emailTaken != null && emailTaken.Id != user.Id)
            throw new InvalidOperationException($"User with email {request.Email} already exists");

        var usernameTaken = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (usernameTaken != null && usernameTaken.Id != user.Id)
            throw new InvalidOperationException($"User with username {request.Username} already exists");

        Apply(user, request);

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.Password = _passwordHasher.HashPassword(request.Password!);

        var entityValidation = user.Validate();
        if (!entityValidation.IsValid)
        {
            var failures = entityValidation.Errors
                .Select(e => new ValidationFailure(nameof(User), string.IsNullOrEmpty(e.Detail) ? e.Error : e.Detail));
            throw new ValidationException(failures);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        return _mapper.Map<GetUserResult>(user);
    }

    private static void Apply(User user, UpdateUserCommand request)
    {
        user.Username = request.Username;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Status = request.Status;
        user.Role = request.Role;
        user.Name = request.Name;
        user.Address = request.Address;
        user.UpdatedAt = DateTime.UtcNow;
    }
}

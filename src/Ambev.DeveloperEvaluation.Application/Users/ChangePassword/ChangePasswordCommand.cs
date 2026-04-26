using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.ChangePassword;

public sealed record ChangePasswordCommand(
    int UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Unit>;

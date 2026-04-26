using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.DeleteUser;

/// <summary>
/// Remove o usuário e retorna o estado persistido antes da exclusão (contrato users-api.md).
/// </summary>
public record DeleteUserCommand : IRequest<GetUserResult>
{
    public int Id { get; }

    public DeleteUserCommand(int id)
    {
        Id = id;
    }
}


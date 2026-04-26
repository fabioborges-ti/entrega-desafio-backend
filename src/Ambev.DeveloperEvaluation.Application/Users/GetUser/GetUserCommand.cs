using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.GetUser;

public record GetUserCommand : IRequest<GetUserResult>
{
    public int Id { get; }

    public GetUserCommand(int id)
    {
        Id = id;
    }
}

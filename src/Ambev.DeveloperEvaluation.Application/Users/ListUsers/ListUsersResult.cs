using Ambev.DeveloperEvaluation.Application.Users.GetUser;

namespace Ambev.DeveloperEvaluation.Application.Users.ListUsers;

/// <summary>
/// Resposta paginada alinhada a GET /users (data, totalItems, currentPage, totalPages).
/// </summary>
public sealed class ListUsersResult
{
    public IReadOnlyList<GetUserResult> Data { get; init; } = Array.Empty<GetUserResult>();

    public int TotalItems { get; init; }

    public int CurrentPage { get; init; }

    public int TotalPages { get; init; }
}

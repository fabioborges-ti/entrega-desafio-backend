using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.ListUsers;

/// <summary>
/// Query parameters: <c>_page</c>, <c>_size</c>, <c>_order</c> (ex.: <c>username asc, email desc</c>).
/// </summary>
public sealed record ListUsersQuery(int Page = 1, int PageSize = 10, string? Order = null) : IRequest<ListUsersResult>;

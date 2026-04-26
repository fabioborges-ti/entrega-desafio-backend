using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users;

/// <summary>
/// Envelope da listagem GET /users (<c>data</c>, <c>totalItems</c>, <c>currentPage</c>, <c>totalPages</c>).
/// </summary>
public sealed class UsersPagedListResponse : ApiResponse
{
    public IReadOnlyList<GetUserResponse> Data { get; set; } = Array.Empty<GetUserResponse>();

    public int TotalItems { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }
}

using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.ListUsers;

public sealed class ListUsersHandler : IRequestHandler<ListUsersQuery, ListUsersResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public ListUsersHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<ListUsersResult> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _userRepository.ListPagedAsync(page, size, request.Order, cancellationToken);
        var totalPages = (int)Math.Ceiling(total / (double)size);

        return new ListUsersResult
        {
            Data = items.Select(u => _mapper.Map<GetUserResult>(u)).ToList(),
            TotalItems = total,
            CurrentPage = page,
            TotalPages = totalPages
        };
    }
}

using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.ListCarts;

public class ListCartsHandler : IRequestHandler<ListCartsCommand, ListCartsResult>
{
    private readonly ICartRepository _cartRepository;
    private readonly IMapper _mapper;

    public ListCartsHandler(ICartRepository cartRepository, IMapper mapper)
    {
        _cartRepository = cartRepository;
        _mapper = mapper;
    }

    public async Task<ListCartsResult> Handle(ListCartsCommand request, CancellationToken cancellationToken)
    {
        var validator = new ListCartsCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var (items, total) = await _cartRepository.ListPagedAsync(
            request.Page,
            request.Size,
            request.Order,
            request.Filters,
            cancellationToken);

        var totalPages = request.Size <= 0 ? 0 : (int)Math.Ceiling(total / (double)request.Size);

        return new ListCartsResult
        {
            Data = items.Select(c => _mapper.Map<CartDto>(c)).ToList(),
            TotalItems = total,
            CurrentPage = request.Page,
            TotalPages = totalPages
        };
    }
}

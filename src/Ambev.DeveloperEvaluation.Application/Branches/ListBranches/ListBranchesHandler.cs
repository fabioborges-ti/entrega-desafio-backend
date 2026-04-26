using Ambev.DeveloperEvaluation.Application.Branches;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.ListBranches;

public class ListBranchesHandler : IRequestHandler<ListBranchesCommand, ListBranchesResult>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;

    public ListBranchesHandler(IBranchRepository branchRepository, IMapper mapper)
    {
        _branchRepository = branchRepository;
        _mapper = mapper;
    }

    public async Task<ListBranchesResult> Handle(ListBranchesCommand request, CancellationToken cancellationToken)
    {
        var validator = new ListBranchesCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var (items, total) = await _branchRepository.ListPagedAsync(request.Page, request.Size, cancellationToken);
        var totalPages = request.Size <= 0 ? 0 : (int)Math.Ceiling(total / (double)request.Size);

        return new ListBranchesResult
        {
            Data = items.Select(b => _mapper.Map<BranchDto>(b)).ToList(),
            TotalItems = total,
            CurrentPage = request.Page,
            TotalPages = totalPages
        };
    }
}

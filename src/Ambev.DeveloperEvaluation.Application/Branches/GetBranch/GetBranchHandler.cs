using Ambev.DeveloperEvaluation.Application.Branches;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.GetBranch;

public class GetBranchHandler : IRequestHandler<GetBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;

    public GetBranchHandler(IBranchRepository branchRepository, IMapper mapper)
    {
        _branchRepository = branchRepository;
        _mapper = mapper;
    }

    public async Task<BranchDto> Handle(GetBranchCommand request, CancellationToken cancellationToken)
    {
        var validator = new GetBranchCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var branch = await _branchRepository.GetByIdAsync(request.Id, cancellationToken);
        if (branch == null)
            throw new KeyNotFoundException($"Filial com ID {request.Id} não encontrada.");

        return _mapper.Map<BranchDto>(branch);
    }
}


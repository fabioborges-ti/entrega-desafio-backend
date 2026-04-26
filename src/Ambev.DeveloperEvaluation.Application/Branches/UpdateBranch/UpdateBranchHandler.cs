using Ambev.DeveloperEvaluation.Application.Branches;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;

public class UpdateBranchHandler : IRequestHandler<UpdateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;

    public UpdateBranchHandler(IBranchRepository branchRepository, IMapper mapper)
    {
        _branchRepository = branchRepository;
        _mapper = mapper;
    }

    public async Task<BranchDto> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var validator = new UpdateBranchCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var branch = await _branchRepository.GetTrackedByIdAsync(request.Id, cancellationToken);
        if (branch == null)
            throw new KeyNotFoundException($"Filial com ID {request.Id} não encontrada.");

        var cnpj = CnpjDigits.Normalize(request.Cnpj);
        if (await _branchRepository.ExistsCnpjAsync(cnpj, request.Id, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.Cnpj),
                    "Já existe uma filial com este CNPJ.")
            });
        }

        branch.Name = request.Name.Trim();
        branch.Cnpj = cnpj;
        branch.LastModifiedAt = DateTime.UtcNow;

        var updated = await _branchRepository.UpdateAsync(branch, cancellationToken);
        return _mapper.Map<BranchDto>(updated);
    }
}


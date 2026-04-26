using Ambev.DeveloperEvaluation.Application.Branches;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;

public class CreateBranchHandler : IRequestHandler<CreateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public CreateBranchHandler(
        IBranchRepository branchRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _branchRepository = branchRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<BranchDto> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var validator = new CreateBranchCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var creator = await _userRepository.GetByIdAsync(request.CreatedByUserId, cancellationToken);
        if (creator == null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.CreatedByUserId),
                    $"Usuário com ID {request.CreatedByUserId} não encontrado.")
            });
        }

        var cnpj = CnpjDigits.Normalize(request.Cnpj);
        if (await _branchRepository.ExistsCnpjAsync(cnpj, null, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.Cnpj),
                    "Já existe uma filial com este CNPJ.")
            });
        }

        var now = DateTime.UtcNow;
        var branch = new Branch
        {
            Name = request.Name.Trim(),
            Cnpj = cnpj,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = now,
            LastModifiedAt = now
        };

        var created = await _branchRepository.CreateAsync(branch, cancellationToken);
        return _mapper.Map<BranchDto>(created);
    }
}





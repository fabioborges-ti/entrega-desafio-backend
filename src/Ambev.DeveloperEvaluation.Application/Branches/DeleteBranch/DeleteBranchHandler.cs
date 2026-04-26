using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.DeleteBranch;

public class DeleteBranchHandler : IRequestHandler<DeleteBranchCommand, DeleteBranchResult>
{
    private readonly IBranchRepository _branchRepository;

    public DeleteBranchHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<DeleteBranchResult> Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        var validator = new DeleteBranchCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var exists = await _branchRepository.GetByIdAsync(request.Id, cancellationToken);
        if (exists == null)
            throw new KeyNotFoundException($"Filial com ID {request.Id} não encontrada.");

        if (await _branchRepository.HasSalesAsync(request.Id, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    string.Empty,
                    "Não é possível excluir a filial: existem vendas vinculadas.")
            });
        }

        var deleted = await _branchRepository.DeleteAsync(request.Id, cancellationToken);
        return new DeleteBranchResult { Deleted = deleted };
    }
}


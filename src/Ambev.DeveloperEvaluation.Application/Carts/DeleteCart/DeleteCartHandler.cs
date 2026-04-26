using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.DeleteCart;

public class DeleteCartHandler : IRequestHandler<DeleteCartCommand, DeleteCartResult>
{
    private readonly ICartRepository _cartRepository;
    private readonly ISaleRepository _saleRepository;

    public DeleteCartHandler(ICartRepository cartRepository, ISaleRepository saleRepository)
    {
        _cartRepository = cartRepository;
        _saleRepository = saleRepository;
    }

    public async Task<DeleteCartResult> Handle(DeleteCartCommand request, CancellationToken cancellationToken)
    {
        var validator = new DeleteCartCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        if (await _saleRepository.ExistsSaleForCartAsync(request.Id, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(DeleteCartCommand.Id),
                    "Não é possível excluir o carrinho: já existe uma venda vinculada a ele.")
            });
        }

        var deleted = await _cartRepository.DeleteAsync(request.Id, cancellationToken);
        if (!deleted)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(DeleteCartCommand.Id),
                    $"Carrinho não encontrado (Id: {request.Id}).")
            });
        }

        return new DeleteCartResult { Message = "Carrinho excluído com sucesso." };
    }
}


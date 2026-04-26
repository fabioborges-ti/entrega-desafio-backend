using System.Diagnostics;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

public class DeleteSaleHandler : IRequestHandler<DeleteSaleCommand, DeleteSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ILogger<DeleteSaleHandler> _logger;

    public DeleteSaleHandler(ISaleRepository saleRepository, ILogger<DeleteSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _logger = logger;
    }

    public async Task<DeleteSaleResult> Handle(DeleteSaleCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using (_logger.BeginScope(new Dictionary<string, object?> { ["SaleId"] = request.Id }))
        {
            var validator = new DeleteSaleValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var success = await _saleRepository.DeleteWithCartAndStockReturnAsync(request.Id, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("Exclusão de venda inexistente SaleId={SaleId}", request.Id);
                throw new KeyNotFoundException($"Venda com ID {request.Id} não encontrada.");
            }

            sw.Stop();
            _logger.LogInformation("Venda excluída SaleId={SaleId} em {ElapsedMs} ms", request.Id, sw.ElapsedMilliseconds);
            return new DeleteSaleResult { Success = true };
        }
    }
}


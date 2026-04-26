using System.Diagnostics;
using Ambev.DeveloperEvaluation.Application.Sales.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

public class CancelSaleHandler : IRequestHandler<CancelSaleCommand, CancelSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleEventPublisher _eventPublisher;
    private readonly ILogger<CancelSaleHandler> _logger;

    public CancelSaleHandler(
        ISaleRepository saleRepository,
        ISaleEventPublisher eventPublisher,
        ILogger<CancelSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<CancelSaleResult> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using (_logger.BeginScope(new Dictionary<string, object?> { ["SaleId"] = request.Id }))
        {
            var validator = new CancelSaleValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var (outcome, saleNumber) = await _saleRepository.CancelWithCartAndStockReturnAsync(request.Id, cancellationToken);

            switch (outcome)
            {
            case CancelSaleOutcome.NotFound:
                _logger.LogWarning("Tentativa de cancelar venda inexistente SaleId={SaleId}", request.Id);
                throw new KeyNotFoundException($"Venda com ID {request.Id} não encontrada.");

            case CancelSaleOutcome.AlreadyCancelled:
                sw.Stop();
                _logger.LogInformation(
                    "Cancelamento de venda idempotente (já cancelada) SaleId={SaleId} em {ElapsedMs} ms",
                    request.Id,
                    sw.ElapsedMilliseconds);
                return new CancelSaleResult { Success = true };

            case CancelSaleOutcome.Cancelled:
                if (!request.SuppressEventPublication)
                {
                    _eventPublisher.PublishSaleCancelled(new SaleCancelledPayload(
                        request.Id,
                        saleNumber ?? string.Empty,
                        DateTime.UtcNow));
                }

                sw.Stop();
                _logger.LogInformation(
                    "Venda cancelada SaleId={SaleId} SaleNumber={SaleNumber} em {ElapsedMs} ms",
                    request.Id,
                    saleNumber,
                    sw.ElapsedMilliseconds);
                return new CancelSaleResult { Success = true };

            default:
                throw new InvalidOperationException($"Outcome de cancelamento desconhecido: {outcome}.");
            }
        }
    }
}


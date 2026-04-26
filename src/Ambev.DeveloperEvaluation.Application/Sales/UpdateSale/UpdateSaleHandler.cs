using System.Diagnostics;
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Events;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, GetSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IMapper _mapper;
    private readonly ISaleEventPublisher _eventPublisher;
    private readonly ILogger<UpdateSaleHandler> _logger;

    public UpdateSaleHandler(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IBranchRepository branchRepository,
        ICartRepository cartRepository,
        IMapper mapper,
        ISaleEventPublisher eventPublisher,
        ILogger<UpdateSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _branchRepository = branchRepository;
        _cartRepository = cartRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<GetSaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["SaleId"] = command.Id,
            ["CartId"] = command.CartId
        }))
        {
        var validator = new UpdateSaleCommandValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException($"Venda com ID {command.Id} não encontrada.");

        try
        {
            await EnsureCustomerExistsAsync(command.CustomerId, cancellationToken);
            await EnsureBranchExistsAsync(command.BranchId, cancellationToken);

            sale.UpdateHeader(command.SaleDate, command.CustomerId, command.BranchId);

            if (!sale.CartId.HasValue)
            {
                throw new ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        SaleSubmissionMessages.PropertyCartId,
                        SaleSubmissionMessages.SaleHasNoLinkedCart(sale.Id))
                });
            }

            var currentCartId = sale.CartId.Value;
            Sale updated;

            if (command.CartId == currentCartId)
            {
                updated = await UpdateKeepingCurrentCartAsync(sale, currentCartId, cancellationToken);
            }
            else
            {
                updated = await UpdateReplacingCartAsync(sale, currentCartId, command.CartId, cancellationToken);
            }

            if (!command.SuppressEventPublication)
            {
                _eventPublisher.PublishSaleModified(new SaleModifiedPayload(
                    updated.Id,
                    updated.SaleNumber,
                    updated.TotalAmount,
                    DateTime.UtcNow));
            }

            sw.Stop();
            _logger.LogInformation(
                "Venda atualizada SaleId={SaleId} SaleNumber={SaleNumber} TotalAmount={TotalAmount} em {ElapsedMs} ms",
                updated.Id,
                updated.SaleNumber,
                updated.TotalAmount,
                sw.ElapsedMilliseconds);

            return _mapper.Map<GetSaleResult>(updated);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Regra de domínio ao atualizar venda SaleId={SaleId}", command.Id);
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(string.Empty, ex.Message)
            });
        }
        }
    }

    private async Task EnsureCustomerExistsAsync(int customerId, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    SaleSubmissionMessages.PropertyCustomerId,
                    SaleSubmissionMessages.CustomerNotRegistered(customerId))
            });
        }
    }

    private async Task EnsureBranchExistsAsync(int branchId, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch == null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    SaleSubmissionMessages.PropertyBranchId,
                    SaleSubmissionMessages.BranchNotRegistered(branchId))
            });
        }
    }

    private async Task<Sale> UpdateKeepingCurrentCartAsync(
        Sale sale,
        int cartId,
        CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(cartId, cancellationToken);
        if (cart == null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    SaleSubmissionMessages.PropertyCartId,
                    SaleSubmissionMessages.CartNotRegistered(cartId))
            });
        }
        // Quando o cart não muda, atualização é apenas de cabeçalho.
        // Evita recriar itens desnecessariamente e reduz conflitos de concorrência.
        return await _saleRepository.UpdateAsync(sale, cancellationToken);
    }

    private async Task<Sale> UpdateReplacingCartAsync(
        Sale sale,
        int oldCartId,
        int newCartId,
        CancellationToken cancellationToken)
    {
        // Cart antigo precisa estar tracked para ser removido junto na transação.
        var oldCart = await _cartRepository.GetTrackedByIdAsync(oldCartId, cancellationToken);
        if (oldCart == null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    SaleSubmissionMessages.PropertyCartId,
                    SaleSubmissionMessages.CartNotRegistered(oldCartId))
            });
        }

        var newCart = await _cartRepository.GetByIdAsync(newCartId, cancellationToken);
        if (newCart == null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    SaleSubmissionMessages.PropertyCartId,
                    SaleSubmissionMessages.CartNotRegistered(newCartId))
            });
        }

        // Ignora a própria venda para permitir o reapontamento sem falso positivo.
        if (await _saleRepository.ExistsSaleForCartAsync(newCartId, sale.Id, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    SaleSubmissionMessages.PropertyCartId,
                    SaleSubmissionMessages.CartAlreadyHasSale(newCartId))
            });
        }

        var itemDtos = CartSaleCommandItemMapper.FromCart(newCart);
        if (itemDtos.Count == 0)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    SaleSubmissionMessages.PropertyCartId,
                    SaleSubmissionMessages.CartHasNoLineItems(newCartId))
            });
        }

        var newItems = await BuildSaleItemsFromCatalogAsync(itemDtos, cancellationToken);
        return await _saleRepository.ReplaceCartAndPersistAsync(
            sale,
            oldCart,
            newCartId,
            newItems,
            cancellationToken);
    }

    private async Task<List<SaleItem>> BuildSaleItemsFromCatalogAsync(
        IReadOnlyList<SaleItemCommandDto> itemDtos,
        CancellationToken cancellationToken)
    {
        var cache = new Dictionary<int, Product>();
        var items = new List<SaleItem>(itemDtos.Count);

        foreach (var dto in itemDtos)
        {
            if (!cache.TryGetValue(dto.ProductId, out var product))
            {
                product = await _productRepository.GetByIdAsync(dto.ProductId, cancellationToken);
                if (product == null)
                {
                    throw new ValidationException(new[]
                    {
                        new FluentValidation.Results.ValidationFailure(
                            SaleSubmissionMessages.PropertyProductId,
                            SaleSubmissionMessages.ProductNotRegistered(dto.ProductId))
                    });
                }

                cache[dto.ProductId] = product;
            }

            items.Add(new SaleItem
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitPrice = product.Price
            });
        }

        return items;
    }
}



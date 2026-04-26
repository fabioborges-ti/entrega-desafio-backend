using System.Diagnostics;
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Events;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, CreateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ISaleEventPublisher _eventPublisher;
    private readonly ILogger<CreateSaleHandler> _logger;

    public CreateSaleHandler(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IBranchRepository branchRepository,
        ICartRepository cartRepository,
        ISaleEventPublisher eventPublisher,
        ILogger<CreateSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _branchRepository = branchRepository;
        _cartRepository = cartRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<CreateSaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CartId"] = command.CartId,
            ["CustomerId"] = command.CustomerId,
            ["BranchId"] = command.BranchId
        }))
        {
            var validator = new CreateSaleCommandValidator();
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            if (!string.IsNullOrWhiteSpace(command.SaleNumber))
            {
                var exists = await _saleRepository.ExistsSaleNumberAsync(command.SaleNumber!, cancellationToken);
                if (exists)
                    throw new ValidationException(new[]
                    {
                        new FluentValidation.Results.ValidationFailure(
                            nameof(command.SaleNumber),
                            "Já existe uma venda cadastrada com este número. Informe outro saleNumber ou deixe o campo em branco para gerar um automaticamente.")
                    });
            }

            try
            {
                var customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
                if (customer == null)
                {
                    throw new ValidationException(new[]
                    {
                        new FluentValidation.Results.ValidationFailure(
                            SaleSubmissionMessages.PropertyCustomerId,
                            SaleSubmissionMessages.CustomerNotRegistered(command.CustomerId))
                    });
                }

                var branch = await _branchRepository.GetByIdAsync(command.BranchId, cancellationToken);
                if (branch == null)
                {
                    throw new ValidationException(new[]
                    {
                        new FluentValidation.Results.ValidationFailure(
                            SaleSubmissionMessages.PropertyBranchId,
                            SaleSubmissionMessages.BranchNotRegistered(command.BranchId))
                    });
                }

                var cart = await _cartRepository.GetByIdAsync(command.CartId, cancellationToken);
                if (cart == null)
                {
                    throw new ValidationException(new[]
                    {
                        new FluentValidation.Results.ValidationFailure(
                            SaleSubmissionMessages.PropertyCartId,
                            SaleSubmissionMessages.CartNotRegistered(command.CartId))
                    });
                }

                if (await _saleRepository.ExistsSaleForCartAsync(command.CartId, cancellationToken))
                {
                    throw new ValidationException(new[]
                    {
                        new FluentValidation.Results.ValidationFailure(
                            SaleSubmissionMessages.PropertyCartId,
                            SaleSubmissionMessages.CartAlreadyHasSale(command.CartId))
                    });
                }

                var itemDtos = CartSaleCommandItemMapper.FromCart(cart);
                if (itemDtos.Count == 0)
                {
                    throw new ValidationException(new[]
                    {
                        new FluentValidation.Results.ValidationFailure(
                            SaleSubmissionMessages.PropertyCartId,
                            SaleSubmissionMessages.CartHasNoLineItems(command.CartId))
                    });
                }

                var items = await BuildSaleItemsFromCatalogAsync(itemDtos, cancellationToken);
                var sale = Sale.Create(
                    command.SaleDate,
                    command.CustomerId,
                    command.BranchId,
                    command.CartId,
                    items,
                    command.SaleNumber);

                var created = await _saleRepository.CreateAsync(sale, cancellationToken);

                if (!command.SuppressEventPublication)
                {
                    _eventPublisher.PublishSaleCreated(new SaleCreatedPayload(
                        created.Id,
                        created.SaleNumber,
                        created.SaleDate,
                        created.TotalAmount,
                        DateTime.UtcNow));
                }

                sw.Stop();
                _logger.LogInformation(
                    "Venda criada SaleId={SaleId} SaleNumber={SaleNumber} TotalAmount={TotalAmount} em {ElapsedMs} ms",
                    created.Id,
                    created.SaleNumber,
                    created.TotalAmount,
                    sw.ElapsedMilliseconds);

                return new CreateSaleResult
                {
                    Id = created.Id,
                    SaleNumber = created.SaleNumber,
                    TotalAmount = created.TotalAmount,
                    CartId = created.CartId!.Value
                };
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Regra de domínio ao criar venda CartId={CartId}", command.CartId);
                throw new ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(string.Empty, ex.Message)
                });
            }
        }
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


using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Branch)
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsSaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Sales.AnyAsync(s => s.SaleNumber == saleNumber, cancellationToken);
    }

    public async Task<bool> ExistsSaleForCartAsync(int cartId, CancellationToken cancellationToken = default)
    {
        return await _context.Sales.AnyAsync(s => s.CartId == cartId, cancellationToken);
    }

    public async Task<bool> ExistsSaleForCartAsync(int cartId, int exceptSaleId, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .AnyAsync(s => s.CartId == cartId && s.Id != exceptSaleId, cancellationToken);
    }

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var sale = await GetByIdAsync(id, cancellationToken);
        if (sale == null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteWithCartAndStockReturnAsync(int id, CancellationToken cancellationToken = default)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sale == null)
            return false;

        Cart? cart = null;
        if (sale.CartId.HasValue)
        {
            cart = await _context.Carts
                .Include(c => c.LineItems)
                .FirstOrDefaultAsync(c => c.Id == sale.CartId.Value, cancellationToken);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // FK Sale->Cart é Restrict: precisamos remover a Sale antes do Cart.
            _context.Sales.Remove(sale);
            await _context.SaveChangesAsync(cancellationToken);

            if (cart != null)
            {
                var totalsByProductId = cart.LineItems
                    .GroupBy(li => li.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(li => li.Quantity));

                foreach (var (productId, totalQuantity) in totalsByProductId)
                {
                    if (totalQuantity == 0)
                        continue;

                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
                    if (inventory == null)
                    {
                        throw new ValidationException(new[]
                        {
                            new ValidationFailure(
                                "Products",
                                $"Inventário não encontrado para restaurar estoque do produto (Id: {productId}).")
                        });
                    }

                    inventory.AvailableQuantity += totalQuantity;
                }

                _context.Carts.Remove(cart);
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(CancelSaleOutcome Outcome, string? SaleNumber)> CancelWithCartAndStockReturnAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sale == null)
            return (CancelSaleOutcome.NotFound, null);

        if (sale.IsCancelled)
            return (CancelSaleOutcome.AlreadyCancelled, sale.SaleNumber);

        Cart? cart = null;
        if (sale.CartId.HasValue)
        {
            cart = await _context.Carts
                .Include(c => c.LineItems)
                .FirstOrDefaultAsync(c => c.Id == sale.CartId.Value, cancellationToken);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1) Marca a venda como cancelada e desvincula do cart antes do remove
            //    (FK Sale->Cart é Restrict; precisamos zerar Sale.CartId antes de remover o cart).
            sale.Cancel();
            sale.CartId = null;
            await _context.SaveChangesAsync(cancellationToken);

            // 2) Estorna estoque agregado por produto e remove o cart, se houver.
            if (cart != null)
            {
                var totalsByProductId = cart.LineItems
                    .GroupBy(li => li.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(li => li.Quantity));

                foreach (var (productId, totalQuantity) in totalsByProductId)
                {
                    if (totalQuantity == 0)
                        continue;

                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
                    if (inventory == null)
                    {
                        throw new ValidationException(new[]
                        {
                            new ValidationFailure(
                                "Products",
                                $"Inventário não encontrado para restaurar estoque do produto (Id: {productId}).")
                        });
                    }

                    inventory.AvailableQuantity += totalQuantity;
                }

                _context.Carts.Remove(cart);
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (CancelSaleOutcome.Cancelled, sale.SaleNumber);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Sale> ReplaceCartAndPersistAsync(
        Sale trackedSale,
        Cart trackedOldCart,
        int newCartId,
        IReadOnlyList<SaleItem> newItems,
        CancellationToken cancellationToken = default)
    {
        var totalsByProductId = trackedOldCart.LineItems
            .GroupBy(li => li.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(li => li.Quantity));

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1) Reaponta a venda para o novo cart e substitui itens.
            //    O SaveChanges abaixo libera o índice único de Sale.CartId no cart antigo,
            //    permitindo a remoção subsequente sem violar a FK Restrict.
            trackedSale.ChangeCart(newCartId);
            trackedSale.ReplaceItems(newItems);
            await _context.SaveChangesAsync(cancellationToken);

            // 2) Estorna estoque agregado do cart antigo.
            foreach (var (productId, totalQuantity) in totalsByProductId)
            {
                if (totalQuantity == 0)
                    continue;

                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
                if (inventory == null)
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure(
                            "Products",
                            $"Inventário não encontrado para restaurar estoque do produto (Id: {productId}).")
                    });
                }

                inventory.AvailableQuantity += totalQuantity;
            }

            // 3) Remove o cart antigo, agora desvinculado da venda.
            _context.Carts.Remove(trackedOldCart);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return trackedSale;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Branch)
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(s => s.SaleDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}



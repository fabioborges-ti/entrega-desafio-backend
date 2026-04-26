using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class CartRepository : ICartRepository
{
    private readonly DefaultContext _context;

    public CartRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Cart> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        string? order,
        CartListFilterCriteria? filters = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Carts
            .AsNoTracking()
            .Include(c => c.LineItems)
            .AsSplitQuery();

        query = ApplyCartFilters(query, filters);
        query = ApplyOrdering(query, order);
        return await ToPageAsync(query, page, pageSize, cancellationToken);
    }

    private static IQueryable<Cart> ApplyCartFilters(IQueryable<Cart> query, CartListFilterCriteria? filters)
    {
        if (filters is not { HasAny: true })
            return query;

        if (filters.EqualId is { } equalId)
            query = query.Where(c => c.Id == equalId);
        if (filters.EqualUserId is { } userId)
            query = query.Where(c => c.UserId == userId);
        if (filters.MinDate is { } minDt)
        {
            var start = minDt.Date;
            query = query.Where(c => c.Date >= start);
        }

        if (filters.MaxDate is { } maxDt)
        {
            var endExclusive = maxDt.Date.AddDays(1);
            query = query.Where(c => c.Date < endExclusive);
        }

        return query;
    }

    public async Task<Cart?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .AsNoTracking()
            .Include(c => c.LineItems)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Cart?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.LineItems)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Cart> CreateAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync(cancellationToken);
        return cart;
    }

    public async Task<Cart> CreateWithInventoryDeductionAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        var totalsByProductId = cart.LineItems
            .GroupBy(li => li.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(li => li.Quantity));

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var (productId, totalQuantity) in totalsByProductId)
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
                if (inventory == null || inventory.AvailableQuantity < totalQuantity)
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure(
                            "Products",
                            $"Quantidade indisponível do produto selecionado (Id: {productId}).")
                    });
                }

                inventory.AvailableQuantity -= totalQuantity;
            }

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return cart;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Cart> UpdateWithInventoryAdjustmentAsync(
        Cart trackedCart,
        IReadOnlyList<CartLineItem> newLineItems,
        CancellationToken cancellationToken = default)
    {
        var oldTotalsByProductId = trackedCart.LineItems
            .GroupBy(li => li.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(li => li.Quantity));

        var newTotalsByProductId = newLineItems
            .GroupBy(li => li.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(li => li.Quantity));

        var allProductIds = oldTotalsByProductId.Keys
            .Union(newTotalsByProductId.Keys)
            .Distinct()
            .ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var productId in allProductIds)
            {
                var oldQuantity = oldTotalsByProductId.GetValueOrDefault(productId, 0);
                var newQuantity = newTotalsByProductId.GetValueOrDefault(productId, 0);
                var delta = newQuantity - oldQuantity;
                if (delta == 0)
                    continue;

                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
                if (inventory == null)
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure(
                            "Products",
                            $"Inventário não encontrado para o produto selecionado (Id: {productId}).")
                    });
                }

                if (delta > 0 && inventory.AvailableQuantity < delta)
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure(
                            "Products",
                            $"Quantidade indisponível do produto selecionado (Id: {productId}).")
                    });
                }

                inventory.AvailableQuantity -= delta;
            }

            var existing = trackedCart.LineItems.ToList();
            foreach (var line in existing)
                trackedCart.LineItems.Remove(line);

            foreach (var newLineItem in newLineItems)
            {
                trackedCart.LineItems.Add(new CartLineItem
                {
                    ProductId = newLineItem.ProductId,
                    Quantity = newLineItem.Quantity
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return trackedCart;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Cart> UpdateAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return cart;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .Include(c => c.LineItems)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cart == null)
            return false;

        var totalsByProductId = cart.LineItems
            .GroupBy(li => li.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(li => li.Quantity));

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var (productId, totalQuantity) in totalsByProductId)
            {
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
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<(IReadOnlyList<Cart> Items, int TotalCount)> ToPageAsync(
        IQueryable<Cart> orderedQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var total = await orderedQuery.CountAsync(cancellationToken);
        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    private static IQueryable<Cart> ApplyOrdering(IQueryable<Cart> query, string? order)
    {
        var specs = ParseOrderSpecs(order);
        if (specs.Count == 0)
            return query.OrderBy(c => c.Id);

        IOrderedQueryable<Cart>? ordered = null;
        for (var i = 0; i < specs.Count; i++)
        {
            var (field, desc) = specs[i];
            ordered = i == 0
                ? ApplyFirstOrder(query, field, desc)
                : ThenByField(ordered!, field, desc);
        }

        return ordered ?? query.OrderBy(c => c.Id);
    }

    private static List<(string Field, bool Desc)> ParseOrderSpecs(string? order)
    {
        var result = new List<(string Field, bool Desc)>();
        if (string.IsNullOrWhiteSpace(order))
            return result;

        var trimmed = order.Trim().Trim('"');
        var segments = trimmed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var tokens = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                continue;

            var field = tokens[0].Trim().ToLowerInvariant();
            if (!IsAllowedField(field))
                continue;

            var desc = tokens.Length >= 2 && tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
            result.Add((field, desc));
        }

        return result;
    }

    private static bool IsAllowedField(string field) =>
        field is "id" or "userid" or "date";

    private static IOrderedQueryable<Cart> ApplyFirstOrder(IQueryable<Cart> q, string field, bool desc) =>
        field switch
        {
            "id" => desc ? q.OrderByDescending(c => c.Id) : q.OrderBy(c => c.Id),
            "userid" => desc ? q.OrderByDescending(c => c.UserId) : q.OrderBy(c => c.UserId),
            "date" => desc ? q.OrderByDescending(c => c.Date) : q.OrderBy(c => c.Date),
            _ => q.OrderBy(c => c.Id)
        };

    private static IOrderedQueryable<Cart> ThenByField(IOrderedQueryable<Cart> q, string field, bool desc) =>
        field switch
        {
            "id" => desc ? q.ThenByDescending(c => c.Id) : q.ThenBy(c => c.Id),
            "userid" => desc ? q.ThenByDescending(c => c.UserId) : q.ThenBy(c => c.UserId),
            "date" => desc ? q.ThenByDescending(c => c.Date) : q.ThenBy(c => c.Date),
            _ => q.ThenBy(c => c.Id)
        };
}


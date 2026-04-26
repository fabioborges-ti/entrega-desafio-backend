using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly DefaultContext _context;

    public ProductRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        string? order,
        ProductListFilterCriteria? filters,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products.AsNoTracking().Include(p => p.Category);
        query = query.ApplyProductFilters(filters);
        query = ApplyOrdering(query, order);
        return await ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListByCategoryIdPagedAsync(
        int categoryId,
        int page,
        int pageSize,
        string? order,
        ProductListFilterCriteria? filters,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId);
        query = query.ApplyProductFilters(filters);
        query = ApplyOrdering(query, order);
        return await ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstAsync(p => p.Id == product.Id, cancellationToken);
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstAsync(p => p.Id == product.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<(IReadOnlyList<Product> Items, int TotalCount)> ToPageAsync(
        IQueryable<Product> orderedQuery,
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

    /// <summary>
    /// Suporta <c>_order</c> no formato da API, ex.: <c>price desc, title asc</c> (ver general-api.md).
    /// </summary>
    private static IQueryable<Product> ApplyOrdering(IQueryable<Product> query, string? order)
    {
        var specs = ParseOrderSpecs(order);
        if (specs.Count == 0)
            return query.OrderBy(p => p.Id);

        IOrderedQueryable<Product>? ordered = null;
        for (var i = 0; i < specs.Count; i++)
        {
            var (field, desc) = specs[i];
            ordered = i == 0
                ? ApplyFirstOrder(query, field, desc)
                : ThenByField(ordered!, field, desc);
        }

        return ordered ?? query.OrderBy(p => p.Id);
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

            var field = NormalizeField(tokens[0]);
            if (!IsAllowedField(field))
                continue;

            var desc = tokens.Length >= 2 && tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
            result.Add((field, desc));
        }

        return result;
    }

    private static string NormalizeField(string raw)
    {
        var f = raw.Trim().ToLowerInvariant();
        if (f.StartsWith("rating.", StringComparison.Ordinal))
            f = f["rating.".Length..];
        return f;
    }

    private static bool IsAllowedField(string field) =>
        field is "id" or "title" or "price" or "description" or "category" or "image" or "rate" or "count";

    private static IOrderedQueryable<Product> ApplyFirstOrder(IQueryable<Product> q, string field, bool desc) =>
        field switch
        {
            "id" => desc ? q.OrderByDescending(p => p.Id) : q.OrderBy(p => p.Id),
            "title" => desc ? q.OrderByDescending(p => p.Title) : q.OrderBy(p => p.Title),
            "price" => desc ? q.OrderByDescending(p => p.Price) : q.OrderBy(p => p.Price),
            "description" => desc ? q.OrderByDescending(p => p.Description) : q.OrderBy(p => p.Description),
            "category" => desc ? q.OrderByDescending(p => p.Category!.Name) : q.OrderBy(p => p.Category!.Name),
            "image" => desc ? q.OrderByDescending(p => p.Image) : q.OrderBy(p => p.Image),
            "rate" => desc
                ? q.OrderByDescending(p => p.UserRatings.Select(r => (decimal?)r.Rate).Average() ?? 0m)
                : q.OrderBy(p => p.UserRatings.Select(r => (decimal?)r.Rate).Average() ?? 0m),
            "count" => desc
                ? q.OrderByDescending(p => p.UserRatings.Count)
                : q.OrderBy(p => p.UserRatings.Count),
            _ => q.OrderBy(p => p.Id)
        };

    private static IOrderedQueryable<Product> ThenByField(IOrderedQueryable<Product> q, string field, bool desc) =>
        field switch
        {
            "id" => desc ? q.ThenByDescending(p => p.Id) : q.ThenBy(p => p.Id),
            "title" => desc ? q.ThenByDescending(p => p.Title) : q.ThenBy(p => p.Title),
            "price" => desc ? q.ThenByDescending(p => p.Price) : q.ThenBy(p => p.Price),
            "description" => desc ? q.ThenByDescending(p => p.Description) : q.ThenBy(p => p.Description),
            "category" => desc ? q.ThenByDescending(p => p.Category!.Name) : q.ThenBy(p => p.Category!.Name),
            "image" => desc ? q.ThenByDescending(p => p.Image) : q.ThenBy(p => p.Image),
            "rate" => desc
                ? q.ThenByDescending(p => p.UserRatings.Select(r => (decimal?)r.Rate).Average() ?? 0m)
                : q.ThenBy(p => p.UserRatings.Select(r => (decimal?)r.Rate).Average() ?? 0m),
            "count" => desc
                ? q.ThenByDescending(p => p.UserRatings.Count)
                : q.ThenBy(p => p.UserRatings.Count),
            _ => q.ThenBy(p => p.Id)
        };
}

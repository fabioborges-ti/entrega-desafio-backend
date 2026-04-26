using Ambev.DeveloperEvaluation.Domain.Entities;

using Ambev.DeveloperEvaluation.Domain.Repositories;

using Microsoft.EntityFrameworkCore;



namespace Ambev.DeveloperEvaluation.ORM.Repositories;



public class InventoryRepository : IInventoryRepository

{

    private const int MaxPageSize = 100;



    private readonly DefaultContext _context;



    public InventoryRepository(DefaultContext context)

    {

        _context = context;

    }



    public async Task<(IReadOnlyList<Inventory> Items, int TotalCount)> ListPagedAsync(

        int page,

        int pageSize,

        string? order,

        CancellationToken cancellationToken = default)

    {

        page = Math.Max(1, page);

        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);



        IQueryable<Inventory> query = _context.Inventories

            .AsNoTracking()

            .Include(i => i.Product)

            .ThenInclude(p => p!.Category);

        query = ApplyOrdering(query, order);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query

            .Skip((page - 1) * pageSize)

            .Take(pageSize)

            .ToListAsync(cancellationToken);



        return (items, totalCount);

    }



    public async Task<Inventory?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>

        await _context.Inventories

            .AsNoTracking()

            .Include(i => i.Product)

            .ThenInclude(p => p!.Category)

            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);



    public async Task<Inventory?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default) =>

        await _context.Inventories

            .Include(i => i.Product)

            .ThenInclude(p => p!.Category)

            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);



    public async Task<bool> ExistsForProductIdAsync(int productId, CancellationToken cancellationToken = default) =>

        await _context.Inventories.AnyAsync(i => i.ProductId == productId, cancellationToken);



    public async Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default) =>

        await _context.Inventories

            .AsNoTracking()

            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);



    public async Task<Inventory> CreateAsync(Inventory inventory, CancellationToken cancellationToken = default)

    {

        await _context.Inventories.AddAsync(inventory, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Inventories

            .AsNoTracking()

            .Include(i => i.Product)

            .ThenInclude(p => p!.Category)

            .FirstAsync(i => i.Id == inventory.Id, cancellationToken);

    }



    public async Task<Inventory> UpdateAsync(Inventory inventory, CancellationToken cancellationToken = default)

    {

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Inventories

            .AsNoTracking()

            .Include(i => i.Product)

            .ThenInclude(p => p!.Category)

            .FirstAsync(i => i.Id == inventory.Id, cancellationToken);

    }



    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)

    {

        var entity = await _context.Inventories.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (entity == null)

            return false;



        _context.Inventories.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return true;

    }



    private static IQueryable<Inventory> ApplyOrdering(IQueryable<Inventory> query, string? order)

    {

        var specs = ParseOrderSpecs(order);

        if (specs.Count == 0)

            return query.OrderBy(i => i.Id);



        IOrderedQueryable<Inventory>? ordered = null;

        for (var i = 0; i < specs.Count; i++)

        {

            var (field, desc) = specs[i];

            ordered = i == 0

                ? ApplyFirstOrder(query, field, desc)

                : ThenByField(ordered!, field, desc);

        }



        return ordered ?? query.OrderBy(i => i.Id);

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



    public async Task<IReadOnlyList<Inventory>> ListBelowAlertAsync(CancellationToken cancellationToken = default) =>
        await _context.Inventories
            .AsNoTracking()
            .Include(i => i.Product)
            .Where(i => i.MinimumStockAlert > 0 && i.AvailableQuantity <= i.MinimumStockAlert)
            .OrderBy(i => i.AvailableQuantity)
            .ToListAsync(cancellationToken);

    private static bool IsAllowedField(string field) =>
        field is "id" or "productid" or "availablequantity" or "minimumstockalert";



    private static IOrderedQueryable<Inventory> ApplyFirstOrder(IQueryable<Inventory> q, string field, bool desc) =>

        field switch

        {

            "id" => desc ? q.OrderByDescending(i => i.Id) : q.OrderBy(i => i.Id),

            "productid" => desc ? q.OrderByDescending(i => i.ProductId) : q.OrderBy(i => i.ProductId),

            "availablequantity" => desc

                ? q.OrderByDescending(i => i.AvailableQuantity)

                : q.OrderBy(i => i.AvailableQuantity),

            _ => q.OrderBy(i => i.Id)

        };



    private static IOrderedQueryable<Inventory> ThenByField(IOrderedQueryable<Inventory> q, string field, bool desc) =>

        field switch

        {

            "id" => desc ? q.ThenByDescending(i => i.Id) : q.ThenBy(i => i.Id),

            "productid" => desc ? q.ThenByDescending(i => i.ProductId) : q.ThenBy(i => i.ProductId),

            "availablequantity" => desc

                ? q.ThenByDescending(i => i.AvailableQuantity)

                : q.ThenBy(i => i.AvailableQuantity),

            _ => q.ThenBy(i => i.Id)

        };

}



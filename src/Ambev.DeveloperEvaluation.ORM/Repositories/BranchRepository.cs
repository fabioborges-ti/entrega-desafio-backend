using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class BranchRepository : IBranchRepository
{
    private const int MaxPageSize = 100;

    private readonly DefaultContext _context;

    public BranchRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Branch?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Branch?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsCnpjAsync(
        string cnpjDigits,
        int? excludeBranchId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Branches.AsNoTracking().Where(b => b.Cnpj == cnpjDigits);
        if (excludeBranchId is { } excludeId)
            query = query.Where(b => b.Id != excludeId);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasSalesAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return await _context.Sales.AnyAsync(s => s.BranchId == branchId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Branch> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, MaxPageSize);

        var query = _context.Branches.AsNoTracking().OrderBy(b => b.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Branch> CreateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        await _context.Branches.AddAsync(branch, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return branch;
    }

    public async Task<Branch> UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return branch;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetTrackedByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        _context.Branches.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}


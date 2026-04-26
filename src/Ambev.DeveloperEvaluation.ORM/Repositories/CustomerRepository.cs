using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private const int MaxPageSize = 100;

    private readonly DefaultContext _context;

    public CustomerRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Customer?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> HasSalesAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Sales.AnyAsync(s => s.CustomerId == customerId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, MaxPageSize);

        var query = _context.Customers.AsNoTracking().OrderBy(c => c.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetTrackedByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        _context.Customers.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}


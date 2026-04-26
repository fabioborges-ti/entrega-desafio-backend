using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

/// <summary>
/// Implementation of IUserRepository using Entity Framework Core
/// </summary>
public class UserRepository : IUserRepository
{
    private const int MaxPageSize = 100;

    private readonly DefaultContext _context;

    public UserRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim();
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Username.ToLower() == normalized.ToLower(),
                cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        string? order,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var baseQuery = _context.Users.AsNoTracking();
        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var ordered = ApplyOrdering(baseQuery, order);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<User?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IQueryable<User> ApplyOrdering(IQueryable<User> source, string? orderSpec)
    {
        var clauses = ParseOrderClauses(orderSpec);
        if (clauses.Count == 0)
            return source.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id);

        IOrderedQueryable<User>? ordered = null;
        foreach (var (field, desc) in clauses)
        {
            ordered = ordered == null
                ? ApplyOrderBy(source, field, desc)
                : ApplyThenBy(ordered, field, desc);
        }

        return (ordered ?? source.OrderByDescending(u => u.CreatedAt)).ThenBy(u => u.Id);
    }

    private static List<(string Field, bool Desc)> ParseOrderClauses(string? spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
            return new List<(string, bool)>();

        var parts = spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var list = new List<(string, bool)>();
        foreach (var part in parts)
        {
            var tokens = part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                continue;

            var field = tokens[0].Trim().ToLowerInvariant();
            var desc = tokens.Length > 1 && tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
            list.Add((field, desc));
        }

        return list;
    }

    private static IOrderedQueryable<User> ApplyOrderBy(IQueryable<User> q, string field, bool desc)
    {
        return field switch
        {
            "id" => desc ? q.OrderByDescending(u => u.Id) : q.OrderBy(u => u.Id),
            "email" => desc ? q.OrderByDescending(u => u.Email) : q.OrderBy(u => u.Email),
            "username" => desc ? q.OrderByDescending(u => u.Username) : q.OrderBy(u => u.Username),
            "phone" => desc ? q.OrderByDescending(u => u.Phone) : q.OrderBy(u => u.Phone),
            "status" => desc ? q.OrderByDescending(u => u.Status) : q.OrderBy(u => u.Status),
            "role" => desc ? q.OrderByDescending(u => u.Role) : q.OrderBy(u => u.Role),
            "createdat" => desc ? q.OrderByDescending(u => u.CreatedAt) : q.OrderBy(u => u.CreatedAt),
            "firstname" => desc ? q.OrderByDescending(u => u.Name.FirstName) : q.OrderBy(u => u.Name.FirstName),
            "lastname" => desc ? q.OrderByDescending(u => u.Name.LastName) : q.OrderBy(u => u.Name.LastName),
            _ => q.OrderByDescending(u => u.CreatedAt)
        };
    }

    private static IOrderedQueryable<User> ApplyThenBy(IOrderedQueryable<User> q, string field, bool desc)
    {
        return field switch
        {
            "id" => desc ? q.ThenByDescending(u => u.Id) : q.ThenBy(u => u.Id),
            "email" => desc ? q.ThenByDescending(u => u.Email) : q.ThenBy(u => u.Email),
            "username" => desc ? q.ThenByDescending(u => u.Username) : q.ThenBy(u => u.Username),
            "phone" => desc ? q.ThenByDescending(u => u.Phone) : q.ThenBy(u => u.Phone),
            "status" => desc ? q.ThenByDescending(u => u.Status) : q.ThenBy(u => u.Status),
            "role" => desc ? q.ThenByDescending(u => u.Role) : q.ThenBy(u => u.Role),
            "createdat" => desc ? q.ThenByDescending(u => u.CreatedAt) : q.ThenBy(u => u.CreatedAt),
            "firstname" => desc ? q.ThenByDescending(u => u.Name.FirstName) : q.ThenBy(u => u.Name.FirstName),
            "lastname" => desc ? q.ThenByDescending(u => u.Name.LastName) : q.ThenBy(u => u.Name.LastName),
            _ => q.ThenBy(u => u.Id)
        };
    }
}

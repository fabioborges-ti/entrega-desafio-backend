using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class ProductRatingRepository : IProductRatingRepository
{
    private readonly DefaultContext _context;

    public ProductRatingRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task AddAsync(int productId, int userId, decimal rate, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        _context.ProductRatings.Add(new ProductUserRating
        {
            ProductId = productId,
            UserId = userId,
            Rate = rate,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, (decimal AverageRate, int Count)>> GetAggregatesByProductIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
            return new Dictionary<int, (decimal, int)>();

        var rows = await _context.ProductRatings
            .AsNoTracking()
            .Where(r => productIds.Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Avg = g.Average(x => x.Rate),
                Cnt = g.Count()
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => x.ProductId,
            x => (Math.Round(x.Avg, 2, MidpointRounding.AwayFromZero), x.Cnt));
    }
}

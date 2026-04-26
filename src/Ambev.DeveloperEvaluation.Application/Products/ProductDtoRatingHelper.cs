namespace Ambev.DeveloperEvaluation.Application.Products;

public static class ProductDtoRatingHelper
{
    public static void ApplyAggregates(
        IEnumerable<ProductDto> items,
        IReadOnlyDictionary<int, (decimal AverageRate, int Count)> aggregates)
    {
        foreach (var dto in items)
        {
            if (aggregates.TryGetValue(dto.Id, out var agg))
            {
                dto.Rating.Rate = agg.AverageRate;
                dto.Rating.Count = agg.Count;
            }
            else
            {
                dto.Rating.Rate = 0;
                dto.Rating.Count = 0;
            }
        }
    }
}

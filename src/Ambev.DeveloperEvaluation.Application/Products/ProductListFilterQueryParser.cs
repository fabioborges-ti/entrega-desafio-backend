using System.Globalization;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Products;

/// <summary>
/// Interpreta query string conforme <c>general-api.md</c> (filtros, _min/_max, curingas em strings).
/// </summary>
public static class ProductListFilterQueryParser
{
    private static readonly HashSet<string> ReservedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "_page", "_size", "_order"
    };

    public static ProductListFilterCriteria? Parse(IReadOnlyDictionary<string, string?> query)
    {
        int? equalId = null, minId = null, maxId = null, equalCount = null, minCount = null, maxCount = null;
        decimal? equalPrice = null, minPrice = null, maxPrice = null, equalRate = null, minRate = null, maxRate = null;
        string? title = null, description = null, category = null, image = null;

        foreach (var (rawKey, rawValue) in query)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
                continue;

            var key = rawKey.Trim();
            if (ReservedKeys.Contains(key))
                continue;

            var value = rawValue?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var keyLower = key.ToLowerInvariant();

            if (keyLower.StartsWith("_min", StringComparison.Ordinal))
            {
                ApplyMin(keyLower, value, ref minPrice, ref minRate, ref minCount, ref minId);
                continue;
            }

            if (keyLower.StartsWith("_max", StringComparison.Ordinal))
            {
                ApplyMax(keyLower, value, ref maxPrice, ref maxRate, ref maxCount, ref maxId);
                continue;
            }

            var field = NormalizeEqualityFieldKey(keyLower);
            switch (field)
            {
                case "id":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                        equalId = id;
                    break;
                case "price":
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                        equalPrice = price;
                    break;
                case "rate":
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                        equalRate = rate;
                    break;
                case "count":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cnt))
                        equalCount = cnt;
                    break;
                case "title":
                    title = value;
                    break;
                case "description":
                    description = value;
                    break;
                case "category":
                    category = value;
                    break;
                case "image":
                    image = value;
                    break;
            }
        }

        var criteria = new ProductListFilterCriteria
        {
            EqualId = equalId,
            MinId = minId,
            MaxId = maxId,
            EqualPrice = equalPrice,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            EqualRate = equalRate,
            MinRate = minRate,
            MaxRate = maxRate,
            EqualCount = equalCount,
            MinCount = minCount,
            MaxCount = maxCount,
            Title = title,
            Description = description,
            Category = category,
            Image = image
        };

        return criteria.HasAny ? criteria : null;
    }

    private static void ApplyMin(
        string keyLower,
        string value,
        ref decimal? minPrice,
        ref decimal? minRate,
        ref int? minCount,
        ref int? minId)
    {
        var suffix = keyLower["_min".Length..];
        switch (suffix)
        {
            case "price" when decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var mp):
                minPrice = mp;
                break;
            case "rate" when decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var mr):
                minRate = mr;
                break;
            case "count" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mc):
                minCount = mc;
                break;
            case "id" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mi):
                minId = mi;
                break;
        }
    }

    private static void ApplyMax(
        string keyLower,
        string value,
        ref decimal? maxPrice,
        ref decimal? maxRate,
        ref int? maxCount,
        ref int? maxId)
    {
        var suffix = keyLower["_max".Length..];
        switch (suffix)
        {
            case "price" when decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var mp):
                maxPrice = mp;
                break;
            case "rate" when decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var mr):
                maxRate = mr;
                break;
            case "count" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mc):
                maxCount = mc;
                break;
            case "id" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mi):
                maxId = mi;
                break;
        }
    }

    private static string NormalizeEqualityFieldKey(string keyLower) =>
        keyLower switch
        {
            "rating.rate" => "rate",
            "rating.count" => "count",
            _ => keyLower
        };
}

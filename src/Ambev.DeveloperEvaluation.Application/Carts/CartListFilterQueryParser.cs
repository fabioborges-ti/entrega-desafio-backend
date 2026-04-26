using System.Globalization;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Carts;

/// <summary>Interpreta query string de listagem de carrinhos conforme <c>general-api.md</c>.</summary>
public static class CartListFilterQueryParser
{
    private static readonly HashSet<string> ReservedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "_page", "_size", "_order"
    };

    public static CartListFilterCriteria? Parse(IReadOnlyDictionary<string, string?> query)
    {
        int? equalId = null, equalUserId = null;
        DateTime? minDate = null, maxDate = null;

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

            if (keyLower.Equals("_mindate", StringComparison.Ordinal))
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var md))
                    minDate = md;
                continue;
            }

            if (keyLower.Equals("_maxdate", StringComparison.Ordinal))
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var xd))
                    maxDate = xd;
                continue;
            }

            switch (keyLower)
            {
                case "id" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id):
                    equalId = id;
                    break;
                case "userid" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uid):
                    equalUserId = uid;
                    break;
            }
        }

        var criteria = new CartListFilterCriteria
        {
            EqualId = equalId,
            EqualUserId = equalUserId,
            MinDate = minDate,
            MaxDate = maxDate
        };

        return criteria.HasAny ? criteria : null;
    }
}

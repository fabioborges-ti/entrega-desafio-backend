using System.Globalization;
using Ambev.DeveloperEvaluation.Application.Carts;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Carts;

public class CartListFilterQueryParserTests
{
    private static IReadOnlyDictionary<string, string?> Q(params (string Key, string? Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

    [Fact(DisplayName = "Parse: query vazia/só reservadas retorna null")]
    public void Parse_EmptyOrReservedOnly_ReturnsNull()
    {
        CartListFilterQueryParser.Parse(Q()).Should().BeNull();
        CartListFilterQueryParser.Parse(Q(
            ("_page", "1"), ("_size", "10"), ("_order", "date")
        )).Should().BeNull();
    }

    [Fact(DisplayName = "Parse: id, userId, _minDate e _maxDate populam corretamente")]
    public void Parse_FullFilters_ParsedCorrectly()
    {
        var min = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var max = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var result = CartListFilterQueryParser.Parse(Q(
            ("id", "5"),
            ("userId", "10"),
            ("_minDate", min.ToString("o", CultureInfo.InvariantCulture)),
            ("_maxDate", max.ToString("o", CultureInfo.InvariantCulture))));

        result.Should().NotBeNull();
        result!.EqualId.Should().Be(5);
        result.EqualUserId.Should().Be(10);
        result.MinDate.Should().Be(min);
        result.MaxDate.Should().Be(max);
        result.HasAny.Should().BeTrue();
    }

    [Fact(DisplayName = "Parse: valores inválidos são ignorados")]
    public void Parse_InvalidValues_AreIgnored()
    {
        var result = CartListFilterQueryParser.Parse(Q(
            ("id", "abc"),
            ("userId", "xyz"),
            ("_minDate", "not-a-date"),
            ("_maxDate", "not-a-date")));

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Parse: chaves vazias e valores em branco são ignorados; chave desconhecida ignorada")]
    public void Parse_EmptyKeysAndUnknown_AreIgnored()
    {
        var result = CartListFilterQueryParser.Parse(Q(
            ("", "x"),
            (" ", "y"),
            ("foo", "bar"),
            ("id", "  ")));

        result.Should().BeNull();
    }
}


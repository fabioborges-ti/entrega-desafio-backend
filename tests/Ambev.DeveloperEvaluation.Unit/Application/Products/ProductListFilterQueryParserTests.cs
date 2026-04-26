using Ambev.DeveloperEvaluation.Application.Products;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

public class ProductListFilterQueryParserTests
{
    private static IReadOnlyDictionary<string, string?> Q(params (string Key, string? Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

    [Fact(DisplayName = "Parse: query vazia retorna null")]
    public void Parse_EmptyQuery_ReturnsNull()
    {
        ProductListFilterQueryParser.Parse(Q()).Should().BeNull();
    }

    [Fact(DisplayName = "Parse: ignora chaves reservadas (_page/_size/_order) e vazias")]
    public void Parse_ReservedAndEmptyKeys_AreIgnored()
    {
        var result = ProductListFilterQueryParser.Parse(Q(
            ("_page", "1"),
            ("_size", "10"),
            ("_order", "title"),
            (" ", "x"),
            ("title", "  ")));

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Parse: campos de igualdade numéricos e de texto")]
    public void Parse_EqualityFields_ParsedCorrectly()
    {
        var result = ProductListFilterQueryParser.Parse(Q(
            ("id", "10"),
            ("price", "12.5"),
            ("rating.rate", "4.5"),
            ("rating.count", "20"),
            ("title", "*camisa*"),
            ("description", "azul"),
            ("category", "Roupas"),
            ("image", "http://x")));

        result.Should().NotBeNull();
        result!.EqualId.Should().Be(10);
        result.EqualPrice.Should().Be(12.5m);
        result.EqualRate.Should().Be(4.5m);
        result.EqualCount.Should().Be(20);
        result.Title.Should().Be("*camisa*");
        result.Description.Should().Be("azul");
        result.Category.Should().Be("Roupas");
        result.Image.Should().Be("http://x");
    }

    [Fact(DisplayName = "Parse: faixa _min* e _max* para campos numéricos")]
    public void Parse_MinMax_ParsedCorrectly()
    {
        var result = ProductListFilterQueryParser.Parse(Q(
            ("_minPrice", "5"),
            ("_maxPrice", "100"),
            ("_minRate", "1.0"),
            ("_maxRate", "5"),
            ("_minCount", "1"),
            ("_maxCount", "999"),
            ("_minId", "1"),
            ("_maxId", "9999")));

        result.Should().NotBeNull();
        result!.MinPrice.Should().Be(5m);
        result.MaxPrice.Should().Be(100m);
        result.MinRate.Should().Be(1m);
        result.MaxRate.Should().Be(5m);
        result.MinCount.Should().Be(1);
        result.MaxCount.Should().Be(999);
        result.MinId.Should().Be(1);
        result.MaxId.Should().Be(9999);
    }

    [Fact(DisplayName = "Parse: valores numéricos inválidos são ignorados (não populam campo)")]
    public void Parse_InvalidNumeric_AreIgnored()
    {
        var result = ProductListFilterQueryParser.Parse(Q(
            ("id", "abc"),
            ("price", "xyz"),
            ("rating.rate", "??"),
            ("rating.count", "two"),
            ("_minPrice", "ten"),
            ("_maxPrice", "ten"),
            ("_minRate", "high"),
            ("_maxRate", "low"),
            ("_minCount", "x"),
            ("_maxCount", "y"),
            ("_minId", "x"),
            ("_maxId", "y"),
            // pelo menos um campo válido para garantir resultado != null
            ("title", "abc")));

        result.Should().NotBeNull();
        result!.EqualId.Should().BeNull();
        result.EqualPrice.Should().BeNull();
        result.EqualRate.Should().BeNull();
        result.EqualCount.Should().BeNull();
        result.MinPrice.Should().BeNull();
        result.MaxPrice.Should().BeNull();
        result.MinRate.Should().BeNull();
        result.MaxRate.Should().BeNull();
        result.MinCount.Should().BeNull();
        result.MaxCount.Should().BeNull();
        result.MinId.Should().BeNull();
        result.MaxId.Should().BeNull();
        result.Title.Should().Be("abc");
    }

    [Fact(DisplayName = "Parse: chave desconhecida é silenciosamente ignorada")]
    public void Parse_UnknownKey_IsIgnored()
    {
        ProductListFilterQueryParser.Parse(Q(("foo", "bar"))).Should().BeNull();
    }

    [Fact(DisplayName = "Parse: trim de chaves e valores; case-insensitive")]
    public void Parse_TrimAndCaseInsensitive()
    {
        var result = ProductListFilterQueryParser.Parse(Q(
            (" Title ", " camisa "),
            ("CATEGORY", "Roupas")));

        result.Should().NotBeNull();
        result!.Title.Should().Be("camisa");
        result.Category.Should().Be("Roupas");
    }

    [Fact(DisplayName = "Parse: sufixo _min/_max desconhecido é ignorado")]
    public void Parse_UnknownMinMaxSuffix_AreIgnored()
    {
        var result = ProductListFilterQueryParser.Parse(Q(
            ("_minFoo", "1"),
            ("_maxFoo", "1")));

        result.Should().BeNull();
    }
}


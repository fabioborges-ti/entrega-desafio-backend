using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

internal static class ProductQueryFilterExtensions
{
    private static string EscapeForLike(string literal) =>
        literal
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private enum StringMatchKind
    {
        Skip,
        Exact,
        ILikePattern
    }

    private static (StringMatchKind Kind, string Pattern) ParseStringFilter(string? raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == "*")
            return (StringMatchKind.Skip, string.Empty);

        if (!raw.Contains('*', StringComparison.Ordinal))
            return (StringMatchKind.Exact, raw);

        var leading = raw.StartsWith('*');
        var trailing = raw.EndsWith('*');

        if (leading && trailing && raw.Length >= 2)
        {
            var core = raw[1..^1];
            return string.IsNullOrEmpty(core)
                ? (StringMatchKind.Skip, string.Empty)
                : (StringMatchKind.ILikePattern, $"%{EscapeForLike(core)}%");
        }

        if (trailing)
        {
            var core = raw[..^1];
            return string.IsNullOrEmpty(core)
                ? (StringMatchKind.Skip, string.Empty)
                : (StringMatchKind.ILikePattern, $"{EscapeForLike(core)}%");
        }

        if (leading)
        {
            var core = raw[1..];
            return string.IsNullOrEmpty(core)
                ? (StringMatchKind.Skip, string.Empty)
                : (StringMatchKind.ILikePattern, $"%{EscapeForLike(core)}");
        }

        var idx = raw.IndexOf('*', StringComparison.Ordinal);
        var left = raw[..idx];
        var right = raw[(idx + 1)..];
        return (StringMatchKind.ILikePattern, $"%{EscapeForLike(left)}%{EscapeForLike(right)}%");
    }

    private static IQueryable<Product> WhereTitle(this IQueryable<Product> query, string? raw)
    {
        var (kind, pattern) = ParseStringFilter(raw);
        return kind switch
        {
            StringMatchKind.Exact => query.Where(p => p.Title.ToLower() == pattern.ToLower()),
            StringMatchKind.ILikePattern => query.Where(p => EF.Functions.ILike(p.Title, pattern, "\\")),
            _ => query
        };
    }

    private static IQueryable<Product> WhereDescription(this IQueryable<Product> query, string? raw)
    {
        var (kind, pattern) = ParseStringFilter(raw);
        return kind switch
        {
            StringMatchKind.Exact => query.Where(p => p.Description.ToLower() == pattern.ToLower()),
            StringMatchKind.ILikePattern => query.Where(p => EF.Functions.ILike(p.Description, pattern, "\\")),
            _ => query
        };
    }

    private static IQueryable<Product> WhereCategory(this IQueryable<Product> query, string? raw)
    {
        var (kind, pattern) = ParseStringFilter(raw);
        return kind switch
        {
            StringMatchKind.Exact => query.Where(p =>
                p.Category != null && p.Category.Name.ToLower() == pattern.ToLower()),
            StringMatchKind.ILikePattern => query.Where(p =>
                p.Category != null && EF.Functions.ILike(p.Category.Name, pattern, "\\")),
            _ => query
        };
    }

    private static IQueryable<Product> WhereImage(this IQueryable<Product> query, string? raw)
    {
        var (kind, pattern) = ParseStringFilter(raw);
        return kind switch
        {
            StringMatchKind.Exact => query.Where(p => p.Image.ToLower() == pattern.ToLower()),
            StringMatchKind.ILikePattern => query.Where(p => EF.Functions.ILike(p.Image, pattern, "\\")),
            _ => query
        };
    }

    public static IQueryable<Product> ApplyProductFilters(
        this IQueryable<Product> query,
        ProductListFilterCriteria? filters)
    {
        if (filters == null || !filters.HasAny)
            return query;

        if (filters.EqualId.HasValue)
            query = query.Where(p => p.Id == filters.EqualId.Value);

        if (filters.MinId.HasValue)
            query = query.Where(p => p.Id >= filters.MinId.Value);
        if (filters.MaxId.HasValue)
            query = query.Where(p => p.Id <= filters.MaxId.Value);

        if (filters.EqualPrice.HasValue)
            query = query.Where(p => p.Price == filters.EqualPrice.Value);
        if (filters.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filters.MinPrice.Value);
        if (filters.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filters.MaxPrice.Value);

        if (filters.EqualRate.HasValue)
        {
            var v = filters.EqualRate.Value;
            query = query.Where(p =>
                (p.UserRatings.Select(r => (decimal?)r.Rate).Average() ?? 0m) == v);
        }

        if (filters.MinRate.HasValue)
        {
            var v = filters.MinRate.Value;
            query = query.Where(p =>
                (p.UserRatings.Select(r => (decimal?)r.Rate).Average() ?? 0m) >= v);
        }

        if (filters.MaxRate.HasValue)
        {
            var v = filters.MaxRate.Value;
            query = query.Where(p =>
                (p.UserRatings.Select(r => (decimal?)r.Rate).Average() ?? 0m) <= v);
        }

        if (filters.EqualCount.HasValue)
        {
            var v = filters.EqualCount.Value;
            query = query.Where(p => p.UserRatings.Count == v);
        }

        if (filters.MinCount.HasValue)
        {
            var v = filters.MinCount.Value;
            query = query.Where(p => p.UserRatings.Count >= v);
        }

        if (filters.MaxCount.HasValue)
        {
            var v = filters.MaxCount.Value;
            query = query.Where(p => p.UserRatings.Count <= v);
        }

        query = query.WhereTitle(filters.Title);
        query = query.WhereDescription(filters.Description);
        query = query.WhereCategory(filters.Category);
        query = query.WhereImage(filters.Image);

        return query;
    }
}

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Critérios de filtro para listagem de produtos (general-api: field=value, *, _min/_max).
/// </summary>
public sealed class ProductListFilterCriteria
{
    public int? EqualId { get; init; }

    public decimal? EqualPrice { get; init; }

    public decimal? EqualRate { get; init; }

    public int? EqualCount { get; init; }

    /// <summary>Valor bruto do query string (pode conter * para curinga).</summary>
    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? Category { get; init; }

    public string? Image { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public decimal? MinRate { get; init; }

    public decimal? MaxRate { get; init; }

    public int? MinCount { get; init; }

    public int? MaxCount { get; init; }

    public int? MinId { get; init; }

    public int? MaxId { get; init; }

    public bool HasAny =>
        EqualId.HasValue || EqualPrice.HasValue || EqualRate.HasValue || EqualCount.HasValue
        || Title != null || Description != null || Category != null || Image != null
        || MinPrice.HasValue || MaxPrice.HasValue || MinRate.HasValue || MaxRate.HasValue
        || MinCount.HasValue || MaxCount.HasValue || MinId.HasValue || MaxId.HasValue;
}


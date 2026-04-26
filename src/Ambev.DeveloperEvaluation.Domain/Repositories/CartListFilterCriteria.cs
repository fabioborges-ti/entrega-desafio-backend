namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Critérios de filtro para listagem de carrinhos (general-api: <c>id</c>, <c>userId</c>, <c>_minDate</c>, <c>_maxDate</c>).
/// </summary>
public sealed class CartListFilterCriteria
{
    public int? EqualId { get; init; }

    public int? EqualUserId { get; init; }

    public DateTime? MinDate { get; init; }

    public DateTime? MaxDate { get; init; }

    public bool HasAny =>
        EqualId.HasValue || EqualUserId.HasValue || MinDate.HasValue || MaxDate.HasValue;
}


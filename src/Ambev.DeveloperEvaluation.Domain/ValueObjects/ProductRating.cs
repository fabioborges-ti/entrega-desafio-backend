namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// Avaliação agregada do produto (contrato JSON: rating.rate / rating.count).
/// </summary>
public class ProductRating
{
    public decimal Rate { get; set; }

    public int Count { get; set; }
}


namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface IProductRatingRepository
{
    /// <summary>Registra uma nova avaliação (o mesmo usuário pode avaliar o mesmo produto várias vezes).</summary>
    Task AddAsync(int productId, int userId, decimal rate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agregados por produto: média de <c>Rate</c> e quantidade de avaliações.
    /// Produtos sem linhas em <c>ProductRatings</c> não aparecem no dicionário.
    /// </summary>
    Task<IReadOnlyDictionary<int, (decimal AverageRate, int Count)>> GetAggregatesByProductIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);
}


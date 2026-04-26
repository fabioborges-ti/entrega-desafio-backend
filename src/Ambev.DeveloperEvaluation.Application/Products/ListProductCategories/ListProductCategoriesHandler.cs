using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProductCategories;

public class ListProductCategoriesHandler : IRequestHandler<ListProductCategoriesQuery, IReadOnlyList<string>>
{
    private readonly ICategoryRepository _categoryRepository;

    public ListProductCategoriesHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public Task<IReadOnlyList<string>> Handle(ListProductCategoriesQuery request, CancellationToken cancellationToken) =>
        _categoryRepository.GetOrderedNamesAsync(cancellationToken);
}

using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProductCategories;

public class ListProductCategoriesQuery : IRequest<IReadOnlyList<string>>
{
}

using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProductsByCategory;

public class ListProductsByCategoryCommand : IRequest<ListProductsResult>
{
    public int CategoryId { get; set; }

    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;

    public string? Order { get; set; }

    public ProductListFilterCriteria? Filters { get; set; }
}

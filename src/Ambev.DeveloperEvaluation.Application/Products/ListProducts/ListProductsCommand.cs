using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

public class ListProductsCommand : IRequest<ListProductsResult>
{
    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;

    public string? Order { get; set; }

    public ProductListFilterCriteria? Filters { get; set; }
}

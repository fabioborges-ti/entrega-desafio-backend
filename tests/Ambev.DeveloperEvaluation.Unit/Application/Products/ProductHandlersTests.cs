using Ambev.DeveloperEvaluation.Application.Products;
using Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;
using Ambev.DeveloperEvaluation.Application.Products.GetProduct;
using Ambev.DeveloperEvaluation.Application.Products.ListProductCategories;
using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

/// <summary>
/// Cobertura dos handlers e validators restantes do módulo Products
/// (GetProduct, DeleteProduct, UpdateProduct, ListProducts, ListProductCategories).
/// </summary>
public class ProductHandlersTests
{
    private static IMapper Mapper()
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<ProductDto>(Arg.Any<Product>()).Returns(ci =>
        {
            var p = ci.Arg<Product>();
            return new ProductDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                Description = p.Description,
                CategoryId = p.CategoryId,
                Category = p.Category?.Name ?? string.Empty,
                Image = p.Image,
                Rating = new ProductRatingDto()
            };
        });
        return mapper;
    }

    // ---------------- GetProduct ----------------

    [Fact(DisplayName = "GetProduct: comando inválido (Id <= 0) lança ValidationException")]
    public async Task GetProduct_InvalidCommand_Throws()
    {
        var handler = new GetProductHandler(
            Substitute.For<IProductRepository>(),
            Substitute.For<IProductRatingRepository>(),
            Mapper());

        var act = async () => await handler.Handle(new GetProductCommand { Id = 0 }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "GetProduct: produto inexistente lança KeyNotFound")]
    public async Task GetProduct_NotFound_Throws()
    {
        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Product?)null);
        var handler = new GetProductHandler(products, Substitute.For<IProductRatingRepository>(), Mapper());

        var act = async () => await handler.Handle(new GetProductCommand { Id = 7 }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "GetProduct: encontrado retorna DTO com agregados aplicados")]
    public async Task GetProduct_Found_ReturnsDtoWithAggregates()
    {
        var product = new Product { Id = 1, Title = "P", Price = 10m, Description = "D", CategoryId = 2, Image = "img" };
        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        var ratings = Substitute.For<IProductRatingRepository>();
        ratings
            .GetAggregatesByProductIdsAsync(
                Arg.Is<IReadOnlyCollection<int>>(ids => ids.Contains(1)),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, (decimal AverageRate, int Count)> { [1] = (4.2m, 5) });

        var handler = new GetProductHandler(products, ratings, Mapper());

        var dto = await handler.Handle(new GetProductCommand { Id = 1 }, CancellationToken.None);

        dto.Id.Should().Be(1);
        dto.Rating.Rate.Should().Be(4.2m);
        dto.Rating.Count.Should().Be(5);
    }

    [Theory(DisplayName = "GetProductCommandValidator: regras")]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-3, false)]
    public void GetProductCommandValidator_ValidationRules(int id, bool expected)
    {
        var v = new GetProductCommandValidator();
        v.Validate(new GetProductCommand { Id = id }).IsValid.Should().Be(expected);
    }

    // ---------------- DeleteProduct ----------------

    [Fact(DisplayName = "DeleteProduct: comando inválido lança ValidationException")]
    public async Task DeleteProduct_InvalidCommand_Throws()
    {
        var handler = new DeleteProductHandler(Substitute.For<IProductRepository>());
        var act = async () => await handler.Handle(new DeleteProductCommand { Id = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "DeleteProduct: inexistente lança KeyNotFound")]
    public async Task DeleteProduct_NotFound_Throws()
    {
        var products = Substitute.For<IProductRepository>();
        products.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteProductHandler(products);

        var act = async () => await handler.Handle(new DeleteProductCommand { Id = 99 }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "DeleteProduct: válido retorna mensagem de sucesso")]
    public async Task DeleteProduct_Valid_ReturnsMessage()
    {
        var products = Substitute.For<IProductRepository>();
        products.DeleteAsync(5, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteProductHandler(products);

        var result = await handler.Handle(new DeleteProductCommand { Id = 5 }, CancellationToken.None);

        result.Message.Should().NotBeNullOrEmpty();
        await products.Received(1).DeleteAsync(5, Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "DeleteProductCommandValidator: regras")]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void DeleteProductCommandValidator_ValidationRules(int id, bool expected)
    {
        var v = new DeleteProductCommandValidator();
        v.Validate(new DeleteProductCommand { Id = id }).IsValid.Should().Be(expected);
    }

    // ---------------- UpdateProduct ----------------

    [Fact(DisplayName = "UpdateProduct: comando inválido lança ValidationException")]
    public async Task UpdateProduct_InvalidCommand_Throws()
    {
        var handler = new UpdateProductHandler(
            Substitute.For<IProductRepository>(),
            Substitute.For<ICategoryRepository>(),
            Substitute.For<IProductRatingRepository>(),
            Mapper());

        var cmd = new UpdateProductCommand { Id = 0, Title = "", Price = -1, Description = "", CategoryId = 0, Image = "" };
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "UpdateProduct: produto inexistente lança KeyNotFound")]
    public async Task UpdateProduct_ProductNotFound_Throws()
    {
        var products = Substitute.For<IProductRepository>();
        products.GetTrackedByIdAsync(10, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var handler = new UpdateProductHandler(
            products,
            Substitute.For<ICategoryRepository>(),
            Substitute.For<IProductRatingRepository>(),
            Mapper());

        var cmd = new UpdateProductCommand { Id = 10, Title = "T", Price = 1, Description = "D", CategoryId = 1, Image = "i" };
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "UpdateProduct: categoria inexistente lança ValidationException")]
    public async Task UpdateProduct_CategoryNotFound_Throws()
    {
        var products = Substitute.For<IProductRepository>();
        products.GetTrackedByIdAsync(10, Arg.Any<CancellationToken>()).Returns(new Product { Id = 10 });

        var categories = Substitute.For<ICategoryRepository>();
        categories.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var handler = new UpdateProductHandler(products, categories, Substitute.For<IProductRatingRepository>(), Mapper());

        var cmd = new UpdateProductCommand { Id = 10, Title = "T", Price = 1, Description = "D", CategoryId = 99, Image = "i" };
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.CategoryId));
    }

    [Fact(DisplayName = "UpdateProduct: válido atualiza, persiste e retorna DTO com agregados")]
    public async Task UpdateProduct_Valid_UpdatesAndReturnsDto()
    {
        var entity = new Product { Id = 10, Title = "Old", Price = 1, Description = "Old", CategoryId = 1, Image = "old" };
        var products = Substitute.For<IProductRepository>();
        products.GetTrackedByIdAsync(10, Arg.Any<CancellationToken>()).Returns(entity);
        products.UpdateAsync(entity, Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Product>());

        var categories = Substitute.For<ICategoryRepository>();
        categories.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(new Category { Id = 2, Name = "Cat" });

        var ratings = Substitute.For<IProductRatingRepository>();
        ratings
            .GetAggregatesByProductIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, (decimal AverageRate, int Count)> { [10] = (3m, 4) });

        var handler = new UpdateProductHandler(products, categories, ratings, Mapper());

        var cmd = new UpdateProductCommand
        {
            Id = 10,
            Title = "New",
            Price = 99m,
            Description = "NewD",
            CategoryId = 2,
            Image = "newImg"
        };

        var dto = await handler.Handle(cmd, CancellationToken.None);

        entity.Title.Should().Be("New");
        entity.Price.Should().Be(99m);
        entity.Description.Should().Be("NewD");
        entity.CategoryId.Should().Be(2);
        entity.Image.Should().Be("newImg");
        dto.Rating.Rate.Should().Be(3m);
        dto.Rating.Count.Should().Be(4);
    }

    [Theory(DisplayName = "UpdateProductCommandValidator: regras")]
    [InlineData(1, "T", 0, "D", 1, "i", true)]
    [InlineData(0, "T", 0, "D", 1, "i", false)]
    [InlineData(1, "", 0, "D", 1, "i", false)]
    [InlineData(1, "T", -1, "D", 1, "i", false)]
    [InlineData(1, "T", 0, "", 1, "i", false)]
    [InlineData(1, "T", 0, "D", 0, "i", false)]
    [InlineData(1, "T", 0, "D", 1, "", false)]
    public void UpdateProductCommandValidator_ValidationRules(
        int id, string title, decimal price, string description, int categoryId, string image, bool expected)
    {
        var v = new UpdateProductCommandValidator();
        var cmd = new UpdateProductCommand
        {
            Id = id,
            Title = title,
            Price = price,
            Description = description,
            CategoryId = categoryId,
            Image = image
        };
        v.Validate(cmd).IsValid.Should().Be(expected);
    }

    // ---------------- ListProducts ----------------

    [Fact(DisplayName = "ListProducts: comando inválido lança ValidationException")]
    public async Task ListProducts_InvalidCommand_Throws()
    {
        var handler = new ListProductsHandler(
            Substitute.For<IProductRepository>(),
            Substitute.For<IProductRatingRepository>(),
            Mapper());

        var act = async () => await handler.Handle(new ListProductsCommand { Page = 0, Size = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "ListProducts: válido retorna paginação e aplica agregados")]
    public async Task ListProducts_Valid_ReturnsResult()
    {
        var products = Substitute.For<IProductRepository>();
        var items = new List<Product>
        {
            new() { Id = 1, Title = "A", Category = new Category { Id = 1, Name = "Cat" } },
            new() { Id = 2, Title = "B", Category = new Category { Id = 1, Name = "Cat" } }
        };
        products
            .ListPagedAsync(1, 10, Arg.Any<string?>(), Arg.Any<ProductListFilterCriteria?>(), Arg.Any<CancellationToken>())
            .Returns((items, 25));

        var ratings = Substitute.For<IProductRatingRepository>();
        ratings
            .GetAggregatesByProductIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, (decimal AverageRate, int Count)>
            {
                [1] = (4m, 2)
            });

        var handler = new ListProductsHandler(products, ratings, Mapper());

        var result = await handler.Handle(new ListProductsCommand { Page = 1, Size = 10, Order = "title" }, CancellationToken.None);

        result.TotalItems.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.CurrentPage.Should().Be(1);
        result.Data.Should().HaveCount(2);
        result.Data[0].Rating.Rate.Should().Be(4m);
        result.Data[1].Rating.Rate.Should().Be(0);
    }

    [Theory(DisplayName = "ListProductsCommandValidator: limites")]
    [InlineData(1, 1, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 10, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void ListProductsCommandValidator_ValidationRules(int page, int size, bool expected)
    {
        var v = new ListProductsCommandValidator();
        v.Validate(new ListProductsCommand { Page = page, Size = size }).IsValid.Should().Be(expected);
    }

    // ---------------- ListProductCategories ----------------

    [Fact(DisplayName = "ListProductCategories: delega para repositório de categorias")]
    public async Task ListProductCategories_DelegatesToRepository()
    {
        var categories = Substitute.For<ICategoryRepository>();
        IReadOnlyList<string> expected = new[] { "Eletrônicos", "Roupas" };
        categories.GetOrderedNamesAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new ListProductCategoriesHandler(categories);

        var result = await handler.Handle(new ListProductCategoriesQuery(), CancellationToken.None);

        result.Should().BeSameAs(expected);
        await categories.Received(1).GetOrderedNamesAsync(Arg.Any<CancellationToken>());
    }
}


using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Sales;

/// <summary>
/// Espelha as validações executadas nos endpoints POST/PUT de <c>SalesController</c> antes do MediatR.
/// </summary>
public class SalesRequestValidatorsTests
{
    private static CreateSaleRequest ValidCreateBody() =>
        new()
        {
            SaleDate = DateTime.UtcNow,
            CustomerId = Random.Shared.Next(1, int.MaxValue),
            BranchId = Random.Shared.Next(1, int.MaxValue),
            CartId = 1
        };

    [Fact]
    public async Task CreateSaleRequestValidator_WhenCustomerIdEmpty_Invalid()
    {
        var v = new CreateSaleRequestValidator();
        var r = ValidCreateBody();
        r.CustomerId = 0;

        var result = await v.ValidateAsync(r);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == SaleSubmissionMessages.PropertyCustomerId);
    }

    [Fact]
    public async Task CreateSaleRequestValidator_WhenBranchIdEmpty_Invalid()
    {
        var v = new CreateSaleRequestValidator();
        var r = ValidCreateBody();
        r.BranchId = 0;

        var result = await v.ValidateAsync(r);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == SaleSubmissionMessages.PropertyBranchId);
    }

    [Fact]
    public async Task CreateSaleRequestValidator_WhenCartIdZero_Invalid()
    {
        var v = new CreateSaleRequestValidator();
        var r = ValidCreateBody();
        r.CartId = 0;

        var result = await v.ValidateAsync(r);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSaleRequest.CartId));
    }

    [Fact]
    public async Task CreateSaleRequestValidator_WhenValid_IsValid()
    {
        var v = new CreateSaleRequestValidator();
        var result = await v.ValidateAsync(ValidCreateBody());
        result.IsValid.Should().BeTrue();
    }

    private static UpdateSaleRequest ValidUpdateBody() =>
        new()
        {
            SaleDate = DateTime.UtcNow,
            CustomerId = Random.Shared.Next(1, int.MaxValue),
            BranchId = Random.Shared.Next(1, int.MaxValue),
            CartId = 1
        };

    [Fact]
    public async Task UpdateSaleRequestValidator_WhenCustomerIdEmpty_Invalid()
    {
        var v = new UpdateSaleRequestValidator();
        var r = ValidUpdateBody();
        r.CustomerId = 0;

        var result = await v.ValidateAsync(r);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == SaleSubmissionMessages.PropertyCustomerId);
    }

    [Fact]
    public async Task UpdateSaleRequestValidator_WhenBranchIdEmpty_Invalid()
    {
        var v = new UpdateSaleRequestValidator();
        var r = ValidUpdateBody();
        r.BranchId = 0;

        var result = await v.ValidateAsync(r);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == SaleSubmissionMessages.PropertyBranchId);
    }

    [Fact]
    public async Task UpdateSaleRequestValidator_WhenCartIdZero_Invalid()
    {
        var v = new UpdateSaleRequestValidator();
        var r = ValidUpdateBody();
        r.CartId = 0;

        var result = await v.ValidateAsync(r);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSaleRequest.CartId));
    }

    [Fact]
    public async Task UpdateSaleRequestValidator_WhenValid_IsValid()
    {
        var v = new UpdateSaleRequestValidator();
        var result = await v.ValidateAsync(ValidUpdateBody());
        result.IsValid.Should().BeTrue();
    }
}





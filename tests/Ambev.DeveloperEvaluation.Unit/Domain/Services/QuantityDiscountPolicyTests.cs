using Ambev.DeveloperEvaluation.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Services;

public class QuantityDiscountPolicyTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 0.10)]
    [InlineData(9, 0.10)]
    [InlineData(10, 0.20)]
    [InlineData(20, 0.20)]
    public void GetDiscountRate_ReturnsExpectedRate(int quantity, decimal expectedRate)
    {
        QuantityDiscountPolicy.GetDiscountRate(quantity).Should().Be(expectedRate);
    }

    [Fact]
    public void ValidateQuantity_ThrowsWhenAbove20()
    {
        var act = () => QuantityDiscountPolicy.ValidateQuantity(21);
        act.Should().Throw<DomainException>()
            .WithMessage("*20*");
    }

    [Fact]
    public void ValidateQuantity_ThrowsWhenBelow1()
    {
        var act = () => QuantityDiscountPolicy.ValidateQuantity(0);
        act.Should().Throw<DomainException>();
    }
}

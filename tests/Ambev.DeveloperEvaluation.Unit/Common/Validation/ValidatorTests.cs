using Ambev.DeveloperEvaluation.Common.Validation;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Validation;

public class ValidatorTests
{
    public class WithoutValidator
    {
        public int Foo { get; set; }
    }

    /// <summary>
    /// O <see cref="Validator"/> usa <see cref="Activator.CreateInstance(Type)"/> sobre
    /// <c>IValidator&lt;T&gt;</c>, o que sempre falha (não é possível instanciar uma interface).
    /// O teste documenta o comportamento atual.
    /// </summary>
    [Fact(DisplayName = "Validator.ValidateAsync sempre lança MissingMethodException por tentar instanciar interface")]
    public async Task ValidateAsync_AlwaysThrowsMissingMethod()
    {
        var act = () => Validator.ValidateAsync(new WithoutValidator());

        await act.Should().ThrowAsync<MissingMethodException>();
    }
}


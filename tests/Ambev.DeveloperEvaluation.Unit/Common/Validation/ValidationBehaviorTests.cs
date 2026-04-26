using Ambev.DeveloperEvaluation.Common.Validation;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Validation;

public class ValidationBehaviorTests
{
    public class FakeRequest : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class FakeRequestValidator : AbstractValidator<FakeRequest>
    {
        public FakeRequestValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }

    [Fact(DisplayName = "Sem validators registrados, executa next normalmente")]
    public async Task Handle_WhenNoValidators_InvokesNextAndReturnsResult()
    {
        var sut = new ValidationBehavior<FakeRequest, string>(Array.Empty<IValidator<FakeRequest>>(), NullLoggerFactory.Instance);
        var request = new FakeRequest { Value = "ok" };
        var nextCalled = false;
        Task<string> Next() { nextCalled = true; return Task.FromResult("response"); }

        var result = await sut.Handle(request, Next, CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().Be("response");
    }

    [Fact(DisplayName = "Quando validação passa, executa next e retorna resultado")]
    public async Task Handle_WhenValidationSucceeds_InvokesNext()
    {
        var validator = new FakeRequestValidator();
        var sut = new ValidationBehavior<FakeRequest, string>(new[] { validator }, NullLoggerFactory.Instance);
        var request = new FakeRequest { Value = "valid" };

        var result = await sut.Handle(request, () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact(DisplayName = "Quando validação falha, lança ValidationException sem chamar next")]
    public async Task Handle_WhenValidationFails_ThrowsValidationException()
    {
        var validator = new FakeRequestValidator();
        var sut = new ValidationBehavior<FakeRequest, string>(new[] { validator }, NullLoggerFactory.Instance);
        var request = new FakeRequest { Value = string.Empty };
        var nextInvoked = false;
        Task<string> Next() { nextInvoked = true; return Task.FromResult("never"); }

        var act = () => sut.Handle(request, Next, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().NotBeEmpty();
        nextInvoked.Should().BeFalse();
    }

    [Fact(DisplayName = "Combina falhas de múltiplos validators")]
    public async Task Handle_WhenMultipleValidatorsFail_AggregatesAllFailures()
    {
        var v1 = Substitute.For<IValidator<FakeRequest>>();
        var v2 = Substitute.For<IValidator<FakeRequest>>();
        v1.ValidateAsync(Arg.Any<ValidationContext<FakeRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("A", "err-a") }));
        v2.ValidateAsync(Arg.Any<ValidationContext<FakeRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("B", "err-b") }));
        var sut = new ValidationBehavior<FakeRequest, string>(new[] { v1, v2 }, NullLoggerFactory.Instance);

        var act = () => sut.Handle(new FakeRequest(), () => Task.FromResult("x"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().HaveCount(2);
        ex.Which.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo("A", "B");
    }
}


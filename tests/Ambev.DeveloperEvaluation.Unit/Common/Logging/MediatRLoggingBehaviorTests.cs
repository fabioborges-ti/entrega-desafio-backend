using Ambev.DeveloperEvaluation.Common.Logging;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Logging;

public class MediatRLoggingBehaviorTests
{
    private sealed class SampleRequest : IRequest<string>
    {
        public int UserId { get; init; }
        public int Page { get; init; }
        public string? Search { get; init; }
        public object? ComplexPayload { get; init; }
        public IEnumerable<int>? Items { get; init; }
    }

    [Fact(DisplayName = "Handle retorna resposta quando next conclui com sucesso")]
    public async Task Handle_WhenSuccess_ReturnsResponse()
    {
        var sut = new MediatRLoggingBehavior<SampleRequest, string>(NullLoggerFactory.Instance);
        var request = new SampleRequest
        {
            UserId = 7,
            Page = 1,
            Search = "abc",
            ComplexPayload = new { X = 1 },
            Items = new[] { 1, 2, 3 }
        };

        var result = await sut.Handle(request, () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact(DisplayName = "Handle propaga ValidationException")]
    public async Task Handle_WhenValidationException_Propagates()
    {
        var sut = new MediatRLoggingBehavior<SampleRequest, string>(NullLoggerFactory.Instance);
        var request = new SampleRequest { UserId = 1 };
        var failures = new[] { new ValidationFailure("UserId", "invalid") };

        var act = () => sut.Handle(
            request,
            () => throw new ValidationException(failures),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "Handle propaga exceções não relacionadas a validação")]
    public async Task Handle_WhenUnexpectedException_Propagates()
    {
        var sut = new MediatRLoggingBehavior<SampleRequest, string>(NullLoggerFactory.Instance);
        var request = new SampleRequest { UserId = 2 };

        var act = () => sut.Handle(
            request,
            () => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

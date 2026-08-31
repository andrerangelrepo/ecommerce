using ECommerce.Application.Behaviors;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.Application.Tests.Behaviors;

/// <summary>
/// Tests the MediatR pipeline behavior that logs start, duration, and outcome of requests.
/// Log content itself is not asserted — it would make the test fragile without proving
/// anything about the behavior's actual contract with the pipeline.
/// </summary>
public sealed class LoggingBehaviorTests
{
    private readonly LoggingBehavior<TestRequest, string> _behavior = new(
        NullLogger<LoggingBehavior<TestRequest, string>>.Instance);

    /// <summary>CT-LOGGING-01: verifies a successful request calls next exactly once and returns its result unaltered.</summary>
    [Fact]
    public async Task Handle_ShouldCallNextOnceAndReturnResult_WhenRequestSucceeds()
    {
        var nextCallCount = 0;
        Task<string> Next()
        {
            nextCallCount++;
            return Task.FromResult("handled");
        }

        var result = await _behavior.Handle(new TestRequest("x"), Next, CancellationToken.None);

        nextCallCount.Should().Be(1);
        result.Should().Be("handled");
    }

    /// <summary>CT-LOGGING-02: verifies an unexpected exception from next propagates unchanged, not swallowed.</summary>
    [Fact]
    public async Task Handle_ShouldRethrowSameException_WhenNextFailsUnexpectedly()
    {
        var expectedException = new InvalidOperationException("boom");
        Task<string> Next() => throw expectedException;

        var act = () => _behavior.Handle(new TestRequest("x"), Next, CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(expectedException);
    }

    /// <summary>CT-LOGGING-03: verifies a ValidationException (classified as a known/Warning failure) is still rethrown unchanged.</summary>
    [Fact]
    public async Task Handle_ShouldRethrowSameException_WhenNextFailsValidation()
    {
        var expectedException = new ValidationException("invalid");
        Task<string> Next() => throw expectedException;

        var act = () => _behavior.Handle(new TestRequest("x"), Next, CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Should().BeSameAs(expectedException);
    }

    /// <summary>CT-LOGGING-04: verifies a known business exception is still rethrown unchanged.</summary>
    [Fact]
    public async Task Handle_ShouldRethrowSameException_WhenNextFailsWithKnownBusinessException()
    {
        var expectedException = new OrderCannotBeCancelledException(OrderStatus.Cancelled);
        Task<string> Next() => throw expectedException;

        var act = () => _behavior.Handle(new TestRequest("x"), Next, CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<OrderCannotBeCancelledException>();
        thrown.Which.Should().BeSameAs(expectedException);
    }

    private sealed record TestRequest(string Value) : IRequest<string>;
}

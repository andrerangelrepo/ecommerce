using ECommerce.Application.Behaviors;
using FluentAssertions;
using MediatR;
using Xunit;

namespace ECommerce.Application.Tests.Behaviors;

/// <summary>
/// Tests the MediatR pipeline behavior that wraps requests in an Activity for tracing.
/// The goal is not to validate the OpenTelemetry SDK — it's to validate that our
/// pipeline contract (call next once, return its result, never swallow exceptions)
/// holds regardless of whether anything is listening to the produced Activity.
/// </summary>
public sealed class TracingBehaviorTests
{
    private readonly TracingBehavior<TestRequest, string> _behavior = new();

    /// <summary>CT-TRACING-01: verifies a successful request calls next exactly once and returns its result unaltered.</summary>
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

    /// <summary>CT-TRACING-02: verifies an exception thrown by next propagates unchanged, not swallowed or transformed.</summary>
    [Fact]
    public async Task Handle_ShouldRethrowSameException_WhenNextFails()
    {
        var expectedException = new InvalidOperationException("boom");
        Task<string> Next() => throw expectedException;

        var act = () => _behavior.Handle(new TestRequest("x"), Next, CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(expectedException);
    }

    private sealed record TestRequest(string Value) : IRequest<string>;
}

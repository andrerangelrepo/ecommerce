using ECommerce.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Xunit;

namespace ECommerce.Application.Tests.Behaviors;

/// <summary>
/// Tests the MediatR pipeline behavior that runs FluentValidation before the handler.
/// </summary>
public sealed class ValidationBehaviorTests
{
    private readonly Mock<IValidator<TestRequest>> _validator = new();

    /// <summary>CT-BEHAVIOR-01: verifies a failed validation throws and never calls the next delegate.</summary>
    [Fact]
    public async Task Handle_ShouldThrowAndNotCallNext_WhenValidationFails()
    {
        _validator
            .Setup(validator => validator.ValidateAsync(
                It.IsAny<ValidationContext<TestRequest>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(
                [new ValidationFailure("Value", "Value is required.")]));

        var nextCallCount = 0;
        Task<string> Next()
        {
            nextCallCount++;
            return Task.FromResult("handled");
        }

        var behavior = new ValidationBehavior<TestRequest, string>([_validator.Object]);

        var act = () => behavior.Handle(new TestRequest("x"), Next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        nextCallCount.Should().Be(0);
    }

    /// <summary>CT-BEHAVIOR-02: verifies a passing validation calls the next delegate exactly once.</summary>
    [Fact]
    public async Task Handle_ShouldCallNextOnce_WhenValidationPasses()
    {
        _validator
            .Setup(validator => validator.ValidateAsync(
                It.IsAny<ValidationContext<TestRequest>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var nextCallCount = 0;
        Task<string> Next()
        {
            nextCallCount++;
            return Task.FromResult("handled");
        }

        var behavior = new ValidationBehavior<TestRequest, string>([_validator.Object]);

        var result = await behavior.Handle(new TestRequest("x"), Next, CancellationToken.None);

        nextCallCount.Should().Be(1);
        result.Should().Be("handled");
    }

    /// <summary>
    /// A minimal request used only to exercise <see cref="ValidationBehavior{TRequest,TResponse}"/>.
    /// Must be at least as accessible as the mocked <see cref="IValidator{T}"/>, or Castle's
    /// dynamic proxy generator (used by Moq) cannot build a proxy for it.
    /// </summary>
    /// <param name="Value">An arbitrary value, unused by the behavior itself.</param>
    public sealed record TestRequest(string Value) : IRequest<string>;
}

using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using FluentAssertions;
using Xunit;

namespace ECommerce.Tests.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Tests validation of the create-order input contract.
/// </summary>
public sealed class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    /// <summary>Verifies that an empty customer identifier is rejected.</summary>
    [Fact]
    public async Task ValidateAsync_ShouldRejectEmptyCustomerId()
    {
        var command = CreateValidCommand() with
        {
            CustomerId = Guid.Empty
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateOrderCommand.CustomerId));
    }

    /// <summary>Verifies that an empty item collection is rejected.</summary>
    [Fact]
    public async Task ValidateAsync_ShouldRejectEmptyItems()
    {
        var command = CreateValidCommand() with
        {
            Items = []
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateOrderCommand.Items));
    }

    /// <summary>Verifies that an empty product name is rejected.</summary>
    [Fact]
    public async Task ValidateAsync_ShouldRejectEmptyProductName()
    {
        var command = CreateCommandWithItem(
            CreateValidItem() with { ProductName = string.Empty });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == "Items[0].ProductName");
    }

    /// <summary>Verifies that non-positive quantities are rejected.</summary>
    /// <param name="quantity">The invalid quantity.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_ShouldRejectNonPositiveQuantity(int quantity)
    {
        var command = CreateCommandWithItem(
            CreateValidItem() with { Quantity = quantity });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == "Items[0].Quantity");
    }

    /// <summary>Verifies that non-positive unit prices are rejected.</summary>
    /// <param name="unitPrice">The invalid unit price.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_ShouldRejectNonPositiveUnitPrice(int unitPrice)
    {
        var command = CreateCommandWithItem(
            CreateValidItem() with { UnitPrice = unitPrice });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == "Items[0].UnitPrice");
    }

    /// <summary>Verifies that a valid payload is accepted.</summary>
    [Fact]
    public async Task ValidateAsync_ShouldAcceptValidPayload()
    {
        var command = CreateValidCommand();

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    private static CreateOrderCommand CreateValidCommand()
    {
        return CreateCommandWithItem(CreateValidItem());
    }

    private static CreateOrderCommand CreateCommandWithItem(
        CreateOrderItemCommand item)
    {
        return new CreateOrderCommand(Guid.NewGuid(), [item]);
    }

    private static CreateOrderItemCommand CreateValidItem()
    {
        return new CreateOrderItemCommand("Keyboard", 1, 100m);
    }
}

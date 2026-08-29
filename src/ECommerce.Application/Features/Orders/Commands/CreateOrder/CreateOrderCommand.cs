using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Represents a request to create an order.
/// </summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Items">The items to include in the order.</param>
public sealed record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderItemCommand> Items)
    : IRequest<CreateOrderResult>;

/// <summary>
/// Represents an item supplied when creating an order.
/// </summary>
/// <param name="ProductName">The product name.</param>
/// <param name="Quantity">The quantity ordered.</param>
/// <param name="UnitPrice">The price of one unit.</param>
public sealed record CreateOrderItemCommand(
    string ProductName,
    int Quantity,
    decimal UnitPrice);

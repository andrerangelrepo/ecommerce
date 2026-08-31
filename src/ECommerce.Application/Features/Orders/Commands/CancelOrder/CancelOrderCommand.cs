using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.CancelOrder;

/// <summary>
/// Represents a request to cancel an order.
/// </summary>
/// <param name="Id">The order identifier.</param>
public sealed record CancelOrderCommand(
    Guid Id)
    : IRequest<CancelOrderResult?>;

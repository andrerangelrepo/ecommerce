using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetOrderById;

/// <summary>
/// Represents a request to fetch an order by its identifier.
/// </summary>
/// <param name="Id">The order identifier.</param>
public sealed record GetOrderByIdQuery(
    Guid Id)
    : IRequest<GetOrderByIdResult?>;

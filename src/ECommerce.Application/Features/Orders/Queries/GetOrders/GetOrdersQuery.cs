using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetOrders;

/// <summary>
/// Represents a request to fetch a paginated list of orders.
/// </summary>
/// <param name="Page">The page number.</param>
/// <param name="PageSize">The number of orders per page.</param>
public sealed record GetOrdersQuery(
    int Page,
    int PageSize)
    : IRequest<GetOrdersResult>;

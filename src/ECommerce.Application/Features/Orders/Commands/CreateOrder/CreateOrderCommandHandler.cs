using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Handles requests to create an order.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CreateOrderCommandHandler"/> class.
/// </remarks>
/// <param name="orderRepository">The order persistence abstraction.</param>
public sealed class CreateOrderCommandHandler(IOrderRepository orderRepository)
    : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly IOrderRepository _orderRepository = orderRepository;

    /// <inheritdoc />
    public async Task<CreateOrderResult> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var orderItems = request.Items
            .Select(item => (
                item.ProductName,
                item.Quantity,
                item.UnitPrice))
            .ToList();

        var order = new Order(
            Guid.NewGuid(),
            request.CustomerId,
            orderItems);

        await _orderRepository.AddAsync(order, cancellationToken);

        return new CreateOrderResult(
            order.Id,
            order.CustomerId,
            order.Status,
            order.CreatedAt,
            order.TotalAmount);
    }
}

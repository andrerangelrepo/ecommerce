using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.CancelOrder;

/// <summary>
/// Handles requests to cancel an order.
/// </summary>
/// <param name="orderRepository">The order persistence abstraction.</param>
public sealed class CancelOrderCommandHandler(IOrderRepository orderRepository)
    : IRequestHandler<CancelOrderCommand, CancelOrderResult?>
{
    /// <inheritdoc />
    public async Task<CancelOrderResult?> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(
            request.Id,
            cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.Cancel();

        await orderRepository.UpdateAsync(order, cancellationToken);

        return new CancelOrderResult(
            order.Id,
            order.Status);
    }
}

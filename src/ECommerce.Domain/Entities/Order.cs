using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Represents a customer order.
/// </summary>
public class Order
{
    private readonly List<OrderItem> _items = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Order"/> class.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="items">The initial order items.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="items"/> contains no items.
    /// </exception>
    public Order(
        Guid id,
        Guid customerId,
        IEnumerable<(string ProductName, int Quantity, decimal UnitPrice)> items)
    {
        var orderItems = items.ToList();

        if (orderItems.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item.");
        }

        Id = id;
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;

        foreach (var item in orderItems)
        {
            AddItem(item.ProductName, item.Quantity, item.UnitPrice);
        }
    }

    /// <summary>Gets the order identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the customer identifier.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>Gets the current order status.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>Gets the date and time when the order was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the items in the order.</summary>
    public IReadOnlyCollection<OrderItem> Items => _items;

    /// <summary>Gets the total amount calculated from the order items.</summary>
    public decimal TotalAmount =>
        _items.Sum(item => item.UnitPrice * item.Quantity);

    /// <summary>Creates and adds an item to the order.</summary>
    /// <param name="productName">The product name.</param>
    /// <param name="quantity">The quantity ordered.</param>
    /// <param name="unitPrice">The price of one unit.</param>
    public void AddItem(
        string productName,
        int quantity,
        decimal unitPrice)
    {
        var item = new OrderItem(
            Guid.NewGuid(),
            Id,
            productName,
            quantity,
            unitPrice);

        _items.Add(item);
    }

    /// <summary>Cancels the order.</summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the order is not pending.
    /// </exception>
    public void Cancel()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException("Only pending orders can be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }
}

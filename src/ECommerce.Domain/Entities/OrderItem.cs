namespace ECommerce.Domain.Entities;

/// <summary>
/// Represents an item in an order.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderItem"/> class.
    /// </summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="productName">The product name.</param>
    /// <param name="quantity">The quantity ordered.</param>
    /// <param name="unitPrice">The price of one unit.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="quantity"/> or <paramref name="unitPrice"/> is not greater than zero.
    /// </exception>
    public OrderItem(
        Guid id,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentException("UnitPrice must be greater than zero.");
        }

        Id = id;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    /// <summary>Gets the item identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the identifier of the order containing the item.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>Gets the product name.</summary>
    public string ProductName { get; private set; }

    /// <summary>Gets the quantity ordered.</summary>
    public int Quantity { get; private set; }

    /// <summary>Gets the price of one unit.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Gets the total price for the item.</summary>
    public decimal TotalPrice => UnitPrice * Quantity;
}

using MassTransit;

namespace Contracts;

/// <summary>
/// Published by OrderService after a new order is successfully saved.
/// ProductService subscribes to this to decrement stock.
/// </summary>
public class OrderPlacedEvent
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

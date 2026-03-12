using MassTransit;

namespace ProductService.Contracts;

/// <summary>
/// Published by OrderService after a new order is successfully saved.
/// ProductService subscribes to this to decrement stock.
/// </summary>
[MessageUrn("order-placed")]
public class OrderPlaced
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

using MassTransit;

namespace OrderService.Contracts;

/// <summary>
/// Published by ProductService when a product's stock reaches zero.
/// OrderService subscribes to this to stop accepting orders for that product.
/// </summary>
[MessageUrn("stock-depleted")]
public class StockDepleted
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
}

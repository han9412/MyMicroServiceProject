using MassTransit;
using Microsoft.EntityFrameworkCore;
using MyMicroServiceProject.Data;
using ProductService.Contracts;

namespace ProductService.Consumers;

/// <summary>
/// Triggered whenever OrderService publishes an OrderPlaced event.
/// Responsibility: subtract the ordered quantity from the product's stock,
/// then publish StockDepleted if stock reaches zero.
/// </summary>
public class OrderPlacedConsumer(AppDbContext db, IPublishEndpoint publishEndpoint)
    : IConsumer<OrderPlaced>
{
    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var msg = context.Message;

        var product = await db.Products.FindAsync(msg.ProductId);
        if (product is null)
        {
            // Product was deleted between order creation and this event arriving — nothing to do
            return;
        }

        // Subtract stock (floor at 0 to avoid negative values)
        product.Stock = Math.Max(0, product.Stock - msg.Quantity);
        await db.SaveChangesAsync();

        // If stock just hit zero, notify OrderService so it can block further orders
        if (product.Stock == 0)
        {
            await publishEndpoint.Publish(new StockDepleted
            {
                ProductId   = product.Id,
                ProductName = product.Name
            });
        }
    }
}

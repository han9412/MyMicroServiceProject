using MassTransit;
using Contracts;
using OrderService.Services;

namespace OrderService.Consumers;

/// <summary>
/// Triggered whenever ProductService publishes a StockDepleted event.
/// Responsibility: record the product ID so POST /orders can reject new orders for it.
/// </summary>
public class StockDepletedConsumer(DepletedProductsTracker tracker) : IConsumer<StockDepletedEvent>
{
    public Task Consume(ConsumeContext<StockDepletedEvent> context)
    {
        tracker.MarkDepleted(context.Message.ProductId);
        return Task.CompletedTask;
    }
}

using MassTransit;
using OrderService.Contracts;
using OrderService.Services;

namespace OrderService.Consumers;

/// <summary>
/// Triggered whenever ProductService publishes a StockDepleted event.
/// Responsibility: record the product ID so POST /orders can reject new orders for it.
/// </summary>
public class StockDepletedConsumer(DepletedProductsTracker tracker) : IConsumer<StockDepleted>
{
    public Task Consume(ConsumeContext<StockDepleted> context)
    {
        tracker.MarkDepleted(context.Message.ProductId);
        return Task.CompletedTask;
    }
}

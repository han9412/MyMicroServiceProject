using System.Collections.Concurrent;

namespace OrderService.Services;

/// <summary>
/// Singleton in-memory store of product IDs whose stock has hit zero.
/// Populated by StockDepletedConsumer when ProductService fires a StockDepleted event.
/// Thread-safe: MarkDepleted is called from the MassTransit consumer thread while
/// IsDepleted is called from HTTP request handler threads.
/// </summary>
public class DepletedProductsTracker
{
    private readonly ConcurrentDictionary<int, byte> _depletedIds = new();

    public void MarkDepleted(int productId) => _depletedIds.TryAdd(productId, 0);

    public bool IsDepleted(int productId) => _depletedIds.ContainsKey(productId);
}

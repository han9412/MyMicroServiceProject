namespace OrderService.Services;

/// <summary>
/// Singleton in-memory store of product IDs whose stock has hit zero.
/// Populated by StockDepletedConsumer when ProductService fires a StockDepleted event.
/// </summary>
public class DepletedProductsTracker
{
    private readonly HashSet<int> _depletedIds = [];

    public void MarkDepleted(int productId) => _depletedIds.Add(productId);

    public bool IsDepleted(int productId) => _depletedIds.Contains(productId);
}

using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Models;

public class Order
{
    public int Id { get; set; }

    /// <summary>References a Product that lives in ProductService.</summary>
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    /// <summary>Snapshot of Price * Quantity at the time the order was placed.</summary>
    [Column(TypeName = "decimal(10, 2)")]
    public decimal TotalPrice { get; set; }

    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
}

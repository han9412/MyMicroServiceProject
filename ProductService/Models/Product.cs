using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyMicroServiceProject.Models;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    [Column(TypeName = "decimal(6, 2)")]
    public decimal Price { get; set; }

    public string Description { get; set; } = null!;

    /// <summary>Available units in stock. Decremented when an order is placed.</summary>
    public int Stock { get; set; } = 0;
}
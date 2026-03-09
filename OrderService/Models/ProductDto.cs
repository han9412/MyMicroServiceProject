namespace OrderService.Models;

/// <summary>
/// Lightweight DTO that mirrors the fields we need from ProductService.
/// We do NOT share the model class — services stay independent.
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Stock { get; set; }
}

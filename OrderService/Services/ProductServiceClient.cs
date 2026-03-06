using System.Net.Http.Json;
using OrderService.Models;

namespace OrderService.Services;

/// <summary>
/// Typed HttpClient that calls ProductService over HTTP.
/// This is the microservice-to-microservice communication pattern.
/// Base address is configured in Program.cs via appsettings "ProductServiceUrl".
/// </summary>
public class ProductServiceClient(HttpClient http)
{
    /// <summary>
    /// Returns null if the product does not exist (404).
    /// Throws on other HTTP errors.
    /// </summary>
    public async Task<ProductDto?> GetProductAsync(int productId)
    {
        var response = await http.GetAsync($"products/{productId}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }
}

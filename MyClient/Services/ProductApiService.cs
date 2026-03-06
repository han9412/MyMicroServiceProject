using System.Net.Http.Json;
using MyClient.Models;

namespace MyClient.Services;

/// <summary>
/// Typed HttpClient that wraps all calls to ProductService REST API.
/// Injected via DI — base address is set once in Program.cs.
/// </summary>
public class ProductApiService(HttpClient http)
{
    // GET /products
    public Task<List<Product>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<Product>>("products");

    // GET /products/{id}
    public Task<Product?> GetByIdAsync(int id) =>
        http.GetFromJsonAsync<Product>($"products/{id}");

    // POST /products
    public async Task<Product?> CreateAsync(Product product)
    {
        var response = await http.PostAsJsonAsync("products", product);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Product>();
    }

    // PUT /products/{id}
    public async Task UpdateAsync(int id, Product product)
    {
        var response = await http.PutAsJsonAsync($"products/{id}", product);
        response.EnsureSuccessStatusCode();
    }

    // DELETE /products/{id}
    public async Task DeleteAsync(int id)
    {
        var response = await http.DeleteAsync($"products/{id}");
        response.EnsureSuccessStatusCode();
    }
}

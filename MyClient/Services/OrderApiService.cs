using System.Net.Http.Json;
using MyClient.Models;

namespace MyClient.Services;

/// <summary>
/// Typed HttpClient that wraps all calls to OrderService REST API.
/// </summary>
public class OrderApiService(HttpClient http)
{
    // GET /orders
    public Task<List<Order>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<Order>>("orders");

    // GET /orders/{id}
    public Task<Order?> GetByIdAsync(int id) =>
        http.GetFromJsonAsync<Order>($"orders/{id}");

    // POST /orders  — body: { productId, quantity }
    public async Task<Order?> CreateAsync(int productId, int quantity)
    {
        var response = await http.PostAsJsonAsync("orders", new { productId, quantity });
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(string.IsNullOrWhiteSpace(error) ? response.ReasonPhrase : error);
        }
        return await response.Content.ReadFromJsonAsync<Order>();
    }

    // DELETE /orders/{id}
    public async Task DeleteAsync(int id)
    {
        var response = await http.DeleteAsync($"orders/{id}");
        response.EnsureSuccessStatusCode();
    }
}

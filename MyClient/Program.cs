using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MyClient;
using MyClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Read service URLs from wwwroot/appsettings.json
var productServiceUrl = builder.Configuration["ProductServiceUrl"]
    ?? "http://localhost:5043";

var orderServiceUrl = builder.Configuration["OrderServiceUrl"]
    ?? "http://localhost:5157";

builder.Services.AddScoped<ProductApiService>(_ =>
    new ProductApiService(new HttpClient { BaseAddress = new Uri(productServiceUrl) }));

builder.Services.AddScoped<OrderApiService>(_ =>
    new OrderApiService(new HttpClient { BaseAddress = new Uri(orderServiceUrl) }));

await builder.Build().RunAsync();

using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Consumers;
using OrderService.Contracts;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

// ── ProductService HTTP client ────────────────────────────────────────────────
// OrderService calls ProductService over HTTP to validate products and snapshot prices.
var productServiceUrl = builder.Configuration["ProductServiceUrl"]
    ?? "http://localhost:5043";

builder.Services.AddHttpClient<ProductServiceClient>(client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
});

// ── Depleted products tracker ───────────────────────────────────────────────────
// Singleton in-memory set — populated by StockDepletedConsumer when an event arrives
builder.Services.AddSingleton<DepletedProductsTracker>();

// ── MassTransit + RabbitMQ ───────────────────────────────────────────────────
// OrderService: publishes OrderPlaced → notifies ProductService
//               consumes StockDepleted → blocks orders for depleted products
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";

builder.Services.AddMassTransit(x =>
{
    // Register the consumer that will handle incoming StockDepleted events
    x.AddConsumer<StockDepletedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(rabbitMqHost, "/", h =>
        {
            h.Username(rabbitMqUsername);
            h.Password(rabbitMqPassword);
        });

        // Bind to shared exchange names so namespace differences don't matter
        cfg.Message<OrderPlaced>(x => x.SetEntityName("order-placed"));
        cfg.Message<StockDepleted>(x => x.SetEntityName("stock-depleted"));

        // Wire up the consumer to its queue
        cfg.ConfigureEndpoints(ctx);
    });
});

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Auto-apply EF migrations at startup — no manual 'dotnet ef database update' needed
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<OrderDbContext>().Database.Migrate();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("AllowAll");

// ── Orders CRUD ───────────────────────────────────────────────────────────────

// GET /orders
app.MapGet("/orders", async (OrderDbContext db) =>
    await db.Orders.OrderByDescending(o => o.OrderedAt).ToListAsync());

// GET /orders/{id}
app.MapGet("/orders/{id}", async (int id, OrderDbContext db) =>
    await db.Orders.FindAsync(id)
        is Order order ? Results.Ok(order) : Results.NotFound());

// POST /orders  — body: { "productId": 1, "quantity": 3 }
app.MapPost("/orders", async (CreateOrderRequest req, OrderDbContext db, ProductServiceClient productClient, IPublishEndpoint publishEndpoint, DepletedProductsTracker depletedTracker) =>
{
    
    // Call ProductService to validate the product exists and get its current price
    var product = await productClient.GetProductAsync(req.ProductId);
    if (product is null)
        return Results.BadRequest($"Product with ID {req.ProductId} does not exist in ProductService.");

    // Reject immediately if we already know this product is out of stock
    if (depletedTracker.IsDepleted(req.ProductId))
        return Results.BadRequest($"Product {product.Name} is out of stock and cannot be ordered.");

    // Synchronous stock check — covers startup/restart scenarios where the
    // StockDepleted event may not have been received yet
    if (product.Stock <= 0)
        return Results.BadRequest($"Product {product.Name} is out of stock.");

    if (req.Quantity <= 0)
        return Results.BadRequest("Quantity must be greater than zero.");

    var order = new Order
    {
        ProductId  = req.ProductId,
        Quantity   = req.Quantity,
        TotalPrice = product.Price * req.Quantity,   // snapshot price at order time
        OrderedAt  = DateTime.UtcNow
    };

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    // Publish event to RabbitMQ — ProductService will consume this and decrement stock
    await publishEndpoint.Publish(new OrderService.Contracts.OrderPlaced
    {
        OrderId   = order.Id,
        ProductId = order.ProductId,
        Quantity  = order.Quantity
    });

    return Results.Created($"/orders/{order.Id}", order);
});

// PUT /orders/{id}  — body: { "quantity": 5 }
app.MapPut("/orders/{id}", async (int id, UpdateOrderRequest req, OrderDbContext db, ProductServiceClient productClient) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    if (req.Quantity <= 0)
        return Results.BadRequest("Quantity must be greater than zero.");

    // Re-fetch current price from ProductService
    var product = await productClient.GetProductAsync(order.ProductId);
    if (product is null)
        return Results.BadRequest($"Product {order.ProductId} no longer exists in ProductService.");

    order.Quantity   = req.Quantity;
    order.TotalPrice = product.Price * req.Quantity;

    await db.SaveChangesAsync();
    return Results.Ok(order);
});

// DELETE /orders/{id}
app.MapDelete("/orders/{id}", async (int id, OrderDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

// ── Request DTOs ──────────────────────────────────────────────────────────────
record CreateOrderRequest(int ProductId, int Quantity);
record UpdateOrderRequest(int Quantity);

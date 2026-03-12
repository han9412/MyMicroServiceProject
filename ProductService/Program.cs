
using MassTransit;
using Microsoft.EntityFrameworkCore;
using MyMicroServiceProject.Data;
using MyMicroServiceProject.Models;
using ProductService.Consumers;
using ProductService.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

// ── MassTransit + RabbitMQ ───────────────────────────────────────────────────
// ProductService: consumes OrderPlaced → decrements stock
//                 publishes StockDepleted → notifies OrderService
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";

builder.Services.AddMassTransit(x =>
{
    // Register the consumer that will handle incoming OrderPlaced events
    x.AddConsumer<OrderPlacedConsumer>();

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

// OpenAPI (Swagger)
builder.Services.AddOpenApi();

// CORS — allow any origin so the Blazor WASM client can call this API
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Auto-apply EF migrations at startup — no manual 'dotnet ef database update' needed
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

if (app.Environment.IsDevelopment())
    app.MapOpenApi(); // available at /openapi/v1.json

app.UseCors("AllowAll");

// ── Products CRUD ────────────────────────────────────────────────────────────

// GET /products
app.MapGet("/products", async (AppDbContext db) =>
    await db.Products.ToListAsync());

// GET /products/{id}
app.MapGet("/products/{id}", async (int id, AppDbContext db) =>
    await db.Products.FindAsync(id)
        is Product product ? Results.Ok(product) : Results.NotFound());

// POST /products
app.MapPost("/products", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{product.Id}", product);
});

// PUT /products/{id}
app.MapPut("/products/{id}", async (int id, Product updated, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name        = updated.Name;
    product.Price       = updated.Price;
    product.Description = updated.Description;
    product.Stock       = updated.Stock;

    await db.SaveChangesAsync();
    return Results.Ok(product);
});

// DELETE /products/{id}
app.MapDelete("/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
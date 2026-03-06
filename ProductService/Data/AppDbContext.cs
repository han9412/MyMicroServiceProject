using Microsoft.EntityFrameworkCore;
using MyMicroServiceProject.Models;

namespace MyMicroServiceProject.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; } = null!;

    // Options are injected by the DI container (configured in Program.cs)
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
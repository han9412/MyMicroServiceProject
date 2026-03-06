using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderService.Data;

/// <summary>
/// Used only by EF Core design-time tools (dotnet ef migrations add / database update).
/// Not loaded at runtime.
/// </summary>
public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(
                "Server=localhost,1434;Database=OrderDb;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;",
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null
                )
            )
            .Options;

        return new OrderDbContext(options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyMicroServiceProject.Data;

/// <summary>
/// Used only by EF Core design-time tools (dotnet ef migrations add / database update).
/// Not loaded at runtime — the DI container handles that via Program.cs.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                "Server=localhost,1433;Database=MyDbName;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;",
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null
                )
            )
            .Options;

        return new AppDbContext(options);
    }
}

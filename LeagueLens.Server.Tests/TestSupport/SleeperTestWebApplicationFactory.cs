using LeagueLens.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LeagueLens.Server.Tests.TestSupport;

// Hosts the real app (real controllers, real DI, real routing) against a SQLite in-memory
// database instead of the SQL Server connection Program.cs configures by default, so
// integration tests don't need a real SQL Server instance.
public sealed class SleeperTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Filename=:memory:");

    public SleeperTestWebApplicationFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Removing only DbContextOptions<T> leaves IDbContextOptionsConfiguration<T> behind
            // — that's the actual holder of Program.cs's UseSqlServer(...) delegate. With it
            // still registered, EF applies BOTH that delegate and our UseSqlite(...) below to
            // the same options builder, tripping EF's "multiple providers registered" guard.
            var efDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<LeagueLensDbContext>)
                    || d.ServiceType == typeof(DbContextOptions)
                    || d.ServiceType == typeof(LeagueLensDbContext)
                    || d.ServiceType == typeof(IDbContextOptionsConfiguration<LeagueLensDbContext>))
                .ToList();
            foreach (var descriptor in efDescriptors)
                services.Remove(descriptor);

            services.AddDbContext<LeagueLensDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeagueLensDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task SeedAsync(Func<LeagueLensDbContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeagueLensDbContext>();
        await seed(db);
        await db.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

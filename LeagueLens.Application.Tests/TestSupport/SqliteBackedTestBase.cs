using LeagueLens.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Application.Tests.TestSupport;

// Uses a real (SQLite) relational engine instead of EF Core's InMemory provider, which
// silently allows things SQL Server would reject (e.g. missing required properties) and so
// doesn't exercise the Fluent API configurations from LeagueLens.Persistence.
public abstract class SqliteBackedTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected LeagueLensDbContext Db { get; }

    protected SqliteBackedTestBase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LeagueLensDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new LeagueLensDbContext(options);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

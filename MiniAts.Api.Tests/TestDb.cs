using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Data;

namespace MiniAts.Api.Tests;

public static class TestDb
{
    public static MiniAtsDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        // The real schema relies on Postgres-only column defaults (gen_random_uuid(), now());
        // Sqlite has neither, so register equivalents before the schema is created.
        // EF Core's Sqlite provider stores Guid columns as TEXT but formats them uppercase
        // (e.g. "3FD5AC8C-..."); Sqlite TEXT comparisons are case-sensitive, so the generated
        // id has to match that casing or lookups by id will silently find nothing.
        connection.CreateFunction("gen_random_uuid", () => Guid.NewGuid().ToString().ToUpperInvariant());
        connection.CreateFunction("now", () => DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffffzzz"));

        var options = new DbContextOptionsBuilder<MiniAtsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new MiniAtsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}

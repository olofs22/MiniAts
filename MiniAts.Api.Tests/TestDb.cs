using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MiniAts.Api.Data;

namespace MiniAts.Api.Tests;

public static class TestDb
{
    private static readonly DateTimeOffsetToBinaryConverter DateTimeOffsetConverter = new();

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
        // Must produce the same binary encoding SqliteTestDbContext stores DateTimeOffset in.
        connection.CreateFunction("now", () => (long)DateTimeOffsetConverter.ConvertToProvider(DateTimeOffset.UtcNow)!);

        var options = new DbContextOptionsBuilder<MiniAtsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SqliteTestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    // Sqlite can't ORDER BY DateTimeOffset (Postgres can); storing it as a long makes
    // production queries that sort by CreatedAt testable without changing them.
    private class SqliteTestDbContext(DbContextOptions<MiniAtsDbContext> options) : MiniAtsDbContext(options)
    {
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }
}

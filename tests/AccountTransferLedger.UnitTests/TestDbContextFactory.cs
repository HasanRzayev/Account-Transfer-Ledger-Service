using System.Data;
using AccountTransferLedger.Infrastructure.Persistence;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AccountTransferLedger.UnitTests;

public static class TestDbContextFactory
{
    static TestDbContextFactory()
    {
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
    }

    public static LedgerDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new LedgerDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static (LedgerDbContext Context, SqliteConnection Connection) CreateSqliteDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new LedgerDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }
}

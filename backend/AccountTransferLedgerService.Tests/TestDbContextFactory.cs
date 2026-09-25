using System.Data;
using AccountTransferLedgerService.Infrastructure.Persistence;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AccountTransferLedgerService.Tests;

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

public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToString();
    }

    public override Guid Parse(object value)
    {
        return value switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var guid) => guid,
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            _ => Guid.Parse(value.ToString()!)
        };
    }
}

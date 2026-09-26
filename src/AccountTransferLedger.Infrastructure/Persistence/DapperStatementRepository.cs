using System.Data;
using AccountTransferLedger.Application.DTOs;
using AccountTransferLedger.Application.Interfaces;
using AccountTransferLedger.Domain.Enums;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AccountTransferLedger.Infrastructure.Persistence;

public class DapperStatementRepository : ILedgerStatementRepository
{
    private readonly LedgerDbContext _dbContext;

    public DapperStatementRepository(LedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetCalculatedBalanceAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        if (!_dbContext.Database.IsRelational())
        {
            var entries = await _dbContext.LedgerEntries
                .Where(l => l.AccountId == accountId)
                .Select(l => l.Amount)
                .ToListAsync(cancellationToken);
            return entries.Sum();
        }

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        const string sql = @"
            SELECT COALESCE(SUM(Amount), 0)
            FROM LedgerEntries
            WHERE AccountId = @AccountId;";
        var transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        return await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql, new { AccountId = accountId }, transaction: transaction, cancellationToken: cancellationToken)
        );
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetCalculatedBalancesAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default)
    {
        var idList = accountIds.ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        if (!_dbContext.Database.IsRelational())
        {
            var entries = await _dbContext.LedgerEntries
                .Where(l => idList.Contains(l.AccountId))
                .Select(l => new { l.AccountId, l.Amount })
                .ToListAsync(cancellationToken);

            return entries
                .GroupBy(e => e.AccountId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        }

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

        const string sql = @"
            SELECT AccountId, COALESCE(SUM(Amount), 0) AS Balance
            FROM LedgerEntries
            WHERE AccountId IN @Ids
            GROUP BY AccountId;";

        var results = await connection.QueryAsync<BalanceRow>(
            new CommandDefinition(sql, new { Ids = idList }, transaction: transaction, cancellationToken: cancellationToken)
        );

        return results.ToDictionary(r => r.AccountId, r => r.Balance);
    }

    public async Task<AccountStatementDto> GetStatementAsync(Guid accountId, StatementQueryRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

        // 1. Get Account Details
        const string accountSql = @"
            SELECT Id, AccountNumber, AccountHolderName, Currency
            FROM Accounts
            WHERE Id = @AccountId;";

        var accountInfo = await connection.QuerySingleOrDefaultAsync<AccountRow>(
            new CommandDefinition(accountSql, new { AccountId = accountId }, transaction: transaction, cancellationToken: cancellationToken)
        );

        if (accountInfo == null)
        {
            return new AccountStatementDto
            {
                AccountId = accountId,
                Entries = Array.Empty<LedgerEntryDto>()
            };
        }

        // 2. Calculate Current Total Balance
        const string balanceSql = @"
            SELECT COALESCE(SUM(Amount), 0)
            FROM LedgerEntries
            WHERE AccountId = @AccountId;";

        var currentBalance = await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(balanceSql, new { AccountId = accountId }, transaction: transaction, cancellationToken: cancellationToken)
        );

        // 3. Count Total Matching Entries
        const string countSql = @"
            SELECT COUNT(1)
            FROM LedgerEntries
            WHERE AccountId = @AccountId
              AND (@FromDate IS NULL OR CreatedAtUtc >= @FromDate)
              AND (@ToDate IS NULL OR CreatedAtUtc <= @ToDate);";

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new 
            { 
                AccountId = accountId,
                FromDate = request.FromDate,
                ToDate = request.ToDate
            }, transaction: transaction, cancellationToken: cancellationToken)
        );

        // 4. Fetch Paginated Statement with Running Balance (Window Function)
        int offset = (Math.Max(1, request.PageNumber) - 1) * Math.Max(1, request.PageSize);
        int limit = Math.Max(1, request.PageSize);

        bool isSqlite = connection.GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);

        string paginatedEntriesSql = isSqlite ? @"
            WITH OrderedEntries AS (
                SELECT 
                    l.Id,
                    l.AccountId,
                    l.TransferId,
                    l.Amount,
                    l.EntryType,
                    l.Description,
                    l.CreatedAtUtc,
                    SUM(l.Amount) OVER (
                        PARTITION BY l.AccountId 
                        ORDER BY l.CreatedAtUtc ASC, l.Id ASC
                        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                    ) as RunningBalance,
                    t.FromAccountId,
                    t.ToAccountId,
                    CASE 
                        WHEN l.EntryType = 1 THEN toAcc.AccountNumber
                        WHEN l.EntryType = 2 THEN fromAcc.AccountNumber
                        ELSE NULL
                    END as CounterpartyAccountNumber,
                    CASE 
                        WHEN l.EntryType = 1 THEN toAcc.AccountHolderName
                        WHEN l.EntryType = 2 THEN fromAcc.AccountHolderName
                        ELSE NULL
                    END as CounterpartyHolderName
                FROM LedgerEntries l
                LEFT JOIN Transfers t ON l.TransferId = t.Id
                LEFT JOIN Accounts fromAcc ON t.FromAccountId = fromAcc.Id
                LEFT JOIN Accounts toAcc ON t.ToAccountId = toAcc.Id
                WHERE l.AccountId = @AccountId
                  AND (@FromDate IS NULL OR l.CreatedAtUtc >= @FromDate)
                  AND (@ToDate IS NULL OR l.CreatedAtUtc <= @ToDate)
            )
            SELECT 
                Id,
                AccountId,
                TransferId,
                Amount,
                EntryType,
                Description,
                RunningBalance,
                CreatedAtUtc,
                CounterpartyAccountNumber,
                CounterpartyHolderName
            FROM OrderedEntries
            ORDER BY CreatedAtUtc DESC, Id DESC
            LIMIT @Limit OFFSET @Offset;" : @"
            WITH OrderedEntries AS (
                SELECT 
                    l.Id,
                    l.AccountId,
                    l.TransferId,
                    l.Amount,
                    l.EntryType,
                    l.Description,
                    l.CreatedAtUtc,
                    SUM(l.Amount) OVER (
                        PARTITION BY l.AccountId 
                        ORDER BY l.CreatedAtUtc ASC, l.Id ASC
                        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                    ) as RunningBalance,
                    t.FromAccountId,
                    t.ToAccountId,
                    CASE 
                        WHEN l.EntryType = 1 THEN toAcc.AccountNumber
                        WHEN l.EntryType = 2 THEN fromAcc.AccountNumber
                        ELSE NULL
                    END as CounterpartyAccountNumber,
                    CASE 
                        WHEN l.EntryType = 1 THEN toAcc.AccountHolderName
                        WHEN l.EntryType = 2 THEN fromAcc.AccountHolderName
                        ELSE NULL
                    END as CounterpartyHolderName
                FROM LedgerEntries l
                LEFT JOIN Transfers t ON l.TransferId = t.Id
                LEFT JOIN Accounts fromAcc ON t.FromAccountId = fromAcc.Id
                LEFT JOIN Accounts toAcc ON t.ToAccountId = toAcc.Id
                WHERE l.AccountId = @AccountId
                  AND (@FromDate IS NULL OR l.CreatedAtUtc >= @FromDate)
                  AND (@ToDate IS NULL OR l.CreatedAtUtc <= @ToDate)
            )
            SELECT 
                Id,
                AccountId,
                TransferId,
                Amount,
                EntryType,
                Description,
                RunningBalance,
                CreatedAtUtc,
                CounterpartyAccountNumber,
                CounterpartyHolderName
            FROM OrderedEntries
            ORDER BY CreatedAtUtc DESC, Id DESC
            OFFSET @Offset ROWS
            FETCH NEXT @Limit ROWS ONLY;";

        var rawEntries = await connection.QueryAsync<StatementRowDto>(
            new CommandDefinition(paginatedEntriesSql, new
            {
                AccountId = accountId,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Limit = limit,
                Offset = offset
            }, transaction: transaction, cancellationToken: cancellationToken)
        );

        var entriesList = rawEntries.Select(r => new LedgerEntryDto
        {
            Id = r.Id,
            AccountId = r.AccountId,
            TransferId = r.TransferId,
            Amount = r.Amount,
            EntryType = (EntryType)r.EntryType,
            Description = r.Description ?? string.Empty,
            RunningBalance = r.RunningBalance,
            CreatedAtUtc = r.CreatedAtUtc,
            CounterpartyAccountNumber = r.CounterpartyAccountNumber,
            CounterpartyHolderName = r.CounterpartyHolderName
        }).ToList();

        return new AccountStatementDto
        {
            AccountId = accountId,
            AccountNumber = accountInfo.AccountNumber,
            AccountHolderName = accountInfo.AccountHolderName,
            Currency = accountInfo.Currency,
            CurrentBalance = currentBalance,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            Entries = entriesList
        };
    }

    private class AccountRow
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
    }

    private class BalanceRow
    {
        public Guid AccountId { get; set; }
        public decimal Balance { get; set; }
    }

    private class StatementRowDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public Guid? TransferId { get; set; }
        public decimal Amount { get; set; }
        public int EntryType { get; set; }
        public string? Description { get; set; }
        public decimal RunningBalance { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? CounterpartyAccountNumber { get; set; }
        public string? CounterpartyHolderName { get; set; }
    }
}

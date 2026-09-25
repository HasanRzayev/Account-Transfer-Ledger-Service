using AccountTransferLedger.Application.DTOs;
using AccountTransferLedger.Infrastructure.Persistence;
using AccountTransferLedger.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountTransferLedger.UnitTests;

public class StatementAndAccountTests
{
    [Fact]
    public async Task CreateAccountAsync_WithInitialBalance_CreatesAccountAndLedgerEntry()
    {
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteDbContext();
        try
        {
            var dapperRepo = new DapperStatementRepository(dbContext);
            var accountService = new AccountService(dbContext, dapperRepo);

            var request = new CreateAccountRequest
            {
                AccountHolderName = "Rəşad Hüseynov",
                InitialBalance = 750.50m,
                Currency = "AZN"
            };

            var created = await accountService.CreateAccountAsync(request);

            Assert.NotNull(created);
            Assert.Equal("Rəşad Hüseynov", created.AccountHolderName);
            Assert.Equal(750.50m, created.Balance);
            Assert.StartsWith("AZ88ACNT", created.AccountNumber);

            var derivedBalance = await dapperRepo.GetCalculatedBalanceAsync(created.Id);
            Assert.Equal(750.50m, derivedBalance);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task GetStatementAsync_ReturnsPaginatedEntriesAndRunningBalance()
    {
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteDbContext();
        try
        {
            var dapperRepo = new DapperStatementRepository(dbContext);
            var accountService = new AccountService(dbContext, dapperRepo);
            var statementService = new StatementService(dapperRepo, dbContext);

            var account = await accountService.CreateAccountAsync(new CreateAccountRequest
            {
                AccountHolderName = "Leyla Qasımova",
                InitialBalance = 1000m
            });

            var statement = await statementService.GetAccountStatementAsync(account.Id, new StatementQueryRequest
            {
                PageNumber = 1,
                PageSize = 10
            });

            Assert.NotNull(statement);
            Assert.Equal(account.Id, statement.AccountId);
            Assert.Equal(1000m, statement.CurrentBalance);
            Assert.Equal(1, statement.TotalCount);
            Assert.Single(statement.Entries);
            Assert.Equal(1000m, statement.Entries[0].RunningBalance);
            Assert.Equal(1000m, statement.Entries[0].Amount);
        }
        finally
        {
            connection.Dispose();
        }
    }
}

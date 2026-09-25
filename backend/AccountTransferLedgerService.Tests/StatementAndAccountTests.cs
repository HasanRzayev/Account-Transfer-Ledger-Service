using AccountTransferLedgerService.Application.DTOs;
using AccountTransferLedgerService.Application.Services;
using AccountTransferLedgerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountTransferLedgerService.Tests;

public class StatementAndAccountTests
{
    [Fact]
    public async Task CreateAccountAsync_WithInitialBalance_CreatesAccountAndLedgerEntry()
    {
        // Arrange
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

            // Act
            var created = await accountService.CreateAccountAsync(request);

            // Assert
            Assert.NotNull(created);
            Assert.Equal("Rəşad Hüseynov", created.AccountHolderName);
            Assert.Equal(750.50m, created.Balance);
            Assert.StartsWith("AZ88ACNT", created.AccountNumber);

            // Verify derived balance from statement repo
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
        // Arrange
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

            // Act
            var statement = await statementService.GetAccountStatementAsync(account.Id, new StatementQueryRequest
            {
                PageNumber = 1,
                PageSize = 10
            });

            // Assert
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

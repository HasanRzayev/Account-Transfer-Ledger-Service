using AccountTransferLedgerService.Application.DTOs;
using AccountTransferLedgerService.Application.Services;
using AccountTransferLedgerService.Domain.Entities;
using AccountTransferLedgerService.Domain.Enums;
using AccountTransferLedgerService.Domain.Exceptions;
using AccountTransferLedgerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccountTransferLedgerService.Tests;

public class TransferServiceUnitTests
{
    [Fact]
    public async Task TransferFundsAsync_ValidTransfer_CreatesTwoAtomicLedgerEntriesAndUpdatesBalances()
    {
        // Arrange
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteDbContext();
        try
        {
            var dapperRepo = new DapperStatementRepository(dbContext);
            var idempotencyService = new IdempotencyService(dbContext);
            var asyncLock = new AccountTransferLedgerService.Infrastructure.Concurrency.KeyedAsyncLock();
            var service = new TransferService(dbContext, idempotencyService, dapperRepo, asyncLock, NullLogger<TransferService>.Instance);

            var sender = new Account { Id = Guid.NewGuid(), AccountNumber = "AZ01TEST1", AccountHolderName = "Azər Məmmədov" };
            var receiver = new Account { Id = Guid.NewGuid(), AccountNumber = "AZ01TEST2", AccountHolderName = "Nigar Əliyeva" };

            await dbContext.Accounts.AddRangeAsync(sender, receiver);
            await dbContext.LedgerEntries.AddAsync(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = sender.Id,
                Amount = 500m,
                EntryType = EntryType.Credit,
                Description = "İlkin balans"
            });
            await dbContext.SaveChangesAsync();

            var request = new TransferRequest
            {
                FromAccountId = sender.Id,
                ToAccountId = receiver.Id,
                Amount = 150m,
                Description = "Xidmət haqqı"
            };

            // Act
            var result = await service.TransferFundsAsync(request, "idemp-key-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(150m, result.Amount);
            Assert.Equal(350m, result.SourceNewBalance);
            Assert.Equal(150m, result.DestinationNewBalance);

            // Verify double-entry ledger entries in database
            var entries = await dbContext.LedgerEntries.Where(l => l.TransferId == result.TransferId).ToListAsync();
            Assert.Equal(2, entries.Count);

            var debit = entries.Single(l => l.EntryType == EntryType.Debit);
            var credit = entries.Single(l => l.EntryType == EntryType.Credit);

            Assert.Equal(sender.Id, debit.AccountId);
            Assert.Equal(-150m, debit.Amount);

            Assert.Equal(receiver.Id, credit.AccountId);
            Assert.Equal(150m, credit.Amount);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task TransferFundsAsync_ZeroOrNegativeAmount_ThrowsInvalidTransferAmountException()
    {
        // Arrange
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteDbContext();
        try
        {
            var dapperRepo = new DapperStatementRepository(dbContext);
            var idempotencyService = new IdempotencyService(dbContext);
            var asyncLock = new AccountTransferLedgerService.Infrastructure.Concurrency.KeyedAsyncLock();
            var service = new TransferService(dbContext, idempotencyService, dapperRepo, asyncLock, NullLogger<TransferService>.Instance);

            var request = new TransferRequest
            {
                FromAccountId = Guid.NewGuid(),
                ToAccountId = Guid.NewGuid(),
                Amount = -50m
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidTransferAmountException>(() => 
                service.TransferFundsAsync(request, "key-zero"));
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task TransferFundsAsync_SameAccount_ThrowsSelfTransferException()
    {
        // Arrange
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteDbContext();
        try
        {
            var dapperRepo = new DapperStatementRepository(dbContext);
            var idempotencyService = new IdempotencyService(dbContext);
            var asyncLock = new AccountTransferLedgerService.Infrastructure.Concurrency.KeyedAsyncLock();
            var service = new TransferService(dbContext, idempotencyService, dapperRepo, asyncLock, NullLogger<TransferService>.Instance);

            var sameId = Guid.NewGuid();
            var request = new TransferRequest
            {
                FromAccountId = sameId,
                ToAccountId = sameId,
                Amount = 100m
            };

            // Act & Assert
            await Assert.ThrowsAsync<SelfTransferException>(() => 
                service.TransferFundsAsync(request, "key-self"));
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task TransferFundsAsync_NonExistentAccount_ThrowsAccountNotFoundException()
    {
        // Arrange
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteDbContext();
        try
        {
            var dapperRepo = new DapperStatementRepository(dbContext);
            var idempotencyService = new IdempotencyService(dbContext);
            var asyncLock = new AccountTransferLedgerService.Infrastructure.Concurrency.KeyedAsyncLock();
            var service = new TransferService(dbContext, idempotencyService, dapperRepo, asyncLock, NullLogger<TransferService>.Instance);

            var sender = new Account { Id = Guid.NewGuid(), AccountNumber = "AZ01TEST1", AccountHolderName = "Azər" };
            await dbContext.Accounts.AddAsync(sender);
            await dbContext.SaveChangesAsync();

            var request = new TransferRequest
            {
                FromAccountId = sender.Id,
                ToAccountId = Guid.NewGuid(), // non-existent
                Amount = 50m
            };

            // Act & Assert
            await Assert.ThrowsAsync<AccountNotFoundException>(() => 
                service.TransferFundsAsync(request, "key-notfound"));
        }
        finally
        {
            connection.Dispose();
        }
    }
}

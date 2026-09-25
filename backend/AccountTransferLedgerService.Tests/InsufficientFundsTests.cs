using AccountTransferLedgerService.Application.DTOs;
using AccountTransferLedgerService.Application.Services;
using AccountTransferLedgerService.Domain.Entities;
using AccountTransferLedgerService.Domain.Enums;
using AccountTransferLedgerService.Domain.Exceptions;
using AccountTransferLedgerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccountTransferLedgerService.Tests;

public class InsufficientFundsTests
{
    [Fact]
    public async Task TransferFundsAsync_WhenAmountExceedsBalance_ThrowsInsufficientFundsExceptionAndRollsBack()
    {
        // Arrange
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteDbContext();
        try
        {
            var dapperRepo = new DapperStatementRepository(dbContext);
            var idempotencyService = new IdempotencyService(dbContext);
            var asyncLock = new AccountTransferLedgerService.Infrastructure.Concurrency.KeyedAsyncLock();
            var service = new TransferService(dbContext, idempotencyService, dapperRepo, asyncLock, NullLogger<TransferService>.Instance);

            var sender = new Account { Id = Guid.NewGuid(), AccountNumber = "AZ01SENDER", AccountHolderName = "Azər" };
            var receiver = new Account { Id = Guid.NewGuid(), AccountNumber = "AZ01RECEIVER", AccountHolderName = "Nigar" };

            await dbContext.Accounts.AddRangeAsync(sender, receiver);
            await dbContext.LedgerEntries.AddAsync(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = sender.Id,
                Amount = 100m,
                EntryType = EntryType.Credit,
                Description = "İlkin balans 100 AZN"
            });
            await dbContext.SaveChangesAsync();

            var request = new TransferRequest
            {
                FromAccountId = sender.Id,
                ToAccountId = receiver.Id,
                Amount = 150m // Exceeds balance of 100m!
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InsufficientFundsException>(() =>
                service.TransferFundsAsync(request, "idemp-overdraft"));

            Assert.Equal("INSUFFICIENT_FUNDS", ex.ErrorCode);
            Assert.Equal(100m, ex.CurrentBalance);
            Assert.Equal(150m, ex.RequestedAmount);

            // Assert that no transfer record or ledger entries were persisted
            var transfersCount = await dbContext.Transfers.CountAsync();
            Assert.Equal(0, transfersCount);

            // Assert sender balance remains exactly 100
            var senderBalance = await dapperRepo.GetCalculatedBalanceAsync(sender.Id);
            Assert.Equal(100m, senderBalance);

            // Assert receiver balance remains exactly 0
            var receiverBalance = await dapperRepo.GetCalculatedBalanceAsync(receiver.Id);
            Assert.Equal(0m, receiverBalance);
        }
        finally
        {
            connection.Dispose();
        }
    }
}

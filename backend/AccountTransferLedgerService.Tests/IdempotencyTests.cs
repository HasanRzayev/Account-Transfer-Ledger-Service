using AccountTransferLedgerService.Application.DTOs;
using AccountTransferLedgerService.Application.Services;
using AccountTransferLedgerService.Domain.Entities;
using AccountTransferLedgerService.Domain.Enums;
using AccountTransferLedgerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccountTransferLedgerService.Tests;

public class IdempotencyTests
{
    [Fact]
    public async Task TransferFundsAsync_WithSameIdempotencyKey_ReturnsCachedResponseWithoutDoubleExecution()
    {
        // Arrange
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteDbContext();
        try
        {
            var dapperRepo = new DapperStatementRepository(dbContext);
            var idempotencyService = new IdempotencyService(dbContext);
            var asyncLock = new AccountTransferLedgerService.Infrastructure.Concurrency.KeyedAsyncLock();
            var service = new TransferService(dbContext, idempotencyService, dapperRepo, asyncLock, NullLogger<TransferService>.Instance);

            var sender = new Account { Id = Guid.NewGuid(), AccountNumber = "AZ01IDEMP1", AccountHolderName = "Azər" };
            var receiver = new Account { Id = Guid.NewGuid(), AccountNumber = "AZ01IDEMP2", AccountHolderName = "Nigar" };

            await dbContext.Accounts.AddRangeAsync(sender, receiver);
            await dbContext.LedgerEntries.AddAsync(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = sender.Id,
                Amount = 1000m,
                EntryType = EntryType.Credit,
                Description = "İlkin depozit"
            });
            await dbContext.SaveChangesAsync();

            var request = new TransferRequest
            {
                FromAccountId = sender.Id,
                ToAccountId = receiver.Id,
                Amount = 200m,
                Description = "İdempotent köçürmə testi"
            };

            const string idempotencyKey = "unique-key-xyz-777";

            // Act 1 - First Execution
            var result1 = await service.TransferFundsAsync(request, idempotencyKey);
            Assert.False(result1.WasCachedResponse);
            Assert.Equal(800m, result1.SourceNewBalance);

            // Act 2 - Retry with Same Key
            var result2 = await service.TransferFundsAsync(request, idempotencyKey);

            // Assert
            Assert.True(result2.WasCachedResponse);
            Assert.Equal(result1.TransferId, result2.TransferId);
            Assert.Equal(result1.Amount, result2.Amount);

            // Verify only ONE transfer entity exists in DB
            var transferCount = await dbContext.Transfers.CountAsync(t => t.IdempotencyKey == idempotencyKey);
            Assert.Equal(1, transferCount);

            // Verify sender balance is exactly 800 (NOT 600)
            var senderBalance = await dapperRepo.GetCalculatedBalanceAsync(sender.Id);
            Assert.Equal(800m, senderBalance);

            // Verify receiver balance is exactly 200 (NOT 400)
            var receiverBalance = await dapperRepo.GetCalculatedBalanceAsync(receiver.Id);
            Assert.Equal(200m, receiverBalance);
        }
        finally
        {
            connection.Dispose();
        }
    }
}

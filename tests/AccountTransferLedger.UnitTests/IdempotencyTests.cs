using AccountTransferLedger.Application.DTOs;
using AccountTransferLedger.Domain.Entities;
using AccountTransferLedger.Domain.Enums;
using AccountTransferLedger.Infrastructure.Concurrency;
using AccountTransferLedger.Infrastructure.Persistence;
using AccountTransferLedger.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccountTransferLedger.UnitTests;

public class IdempotencyTests
{
    [Fact]
    public async Task TransferFundsAsync_WithSameIdempotencyKey_ReturnsCachedResponseWithoutDoubleExecution()
    {
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteDbContext();
        try
        {
            var dapperRepo = new DapperStatementRepository(dbContext);
            var idempotencyService = new IdempotencyService(dbContext);
            var asyncLock = new KeyedAsyncLock();
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

            var result1 = await service.TransferFundsAsync(request, idempotencyKey);
            Assert.False(result1.WasCachedResponse);
            Assert.Equal(800m, result1.SourceNewBalance);

            var result2 = await service.TransferFundsAsync(request, idempotencyKey);

            Assert.True(result2.WasCachedResponse);
            Assert.Equal(result1.TransferId, result2.TransferId);
            Assert.Equal(result1.Amount, result2.Amount);

            var transferCount = await dbContext.Transfers.CountAsync(t => t.IdempotencyKey == idempotencyKey);
            Assert.Equal(1, transferCount);

            var senderBalance = await dapperRepo.GetCalculatedBalanceAsync(sender.Id);
            Assert.Equal(800m, senderBalance);

            var receiverBalance = await dapperRepo.GetCalculatedBalanceAsync(receiver.Id);
            Assert.Equal(200m, receiverBalance);
        }
        finally
        {
            connection.Dispose();
        }
    }
}

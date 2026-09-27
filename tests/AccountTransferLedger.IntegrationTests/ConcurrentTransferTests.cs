using AccountTransferLedger.Application.DTOs;
using AccountTransferLedger.Domain.Entities;
using AccountTransferLedger.Domain.Enums;
using AccountTransferLedger.Domain.Exceptions;
using AccountTransferLedger.Infrastructure.Concurrency;
using AccountTransferLedger.Infrastructure.Persistence;
using AccountTransferLedger.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccountTransferLedger.IntegrationTests;

public class ConcurrentTransferTests
{
    [Fact]
    public async Task ConcurrentTransfers_DebitingSameAccount_NeverAllowsNegativeBalance()
    {
        var dbName = "ConcurrentTestDb_" + Guid.NewGuid().ToString("N");
        
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using (var setupContext = new LedgerDbContext(options))
        {
            var sender = new Account { Id = senderId, AccountNumber = "AZ01SENDER", AccountHolderName = "Azər Məmmədov" };
            var receiver = new Account { Id = receiverId, AccountNumber = "AZ01RECEIVER", AccountHolderName = "Nigar Əliyeva" };

            await setupContext.Accounts.AddRangeAsync(sender, receiver);
            await setupContext.LedgerEntries.AddAsync(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = senderId,
                Amount = 100m,
                EntryType = EntryType.Credit,
                Description = "İlkin balans"
            });
            await setupContext.SaveChangesAsync();
        }

        int concurrentCount = 10;
        decimal amountPerTransfer = 20m;
        var tasks = new List<Task<bool>>();
        var sharedAsyncLock = new KeyedAsyncLock();

        using var barrier = new SemaphoreSlim(0, concurrentCount);

        for (int i = 0; i < concurrentCount; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                await barrier.WaitAsync();

                using var workerContext = new LedgerDbContext(options);
                var dapperRepo = new DapperStatementRepository(workerContext);
                var idempotencyService = new IdempotencyService(workerContext);
                var service = new TransferService(workerContext, idempotencyService, dapperRepo, sharedAsyncLock, NullLogger<TransferService>.Instance);

                try
                {
                    var result = await service.TransferFundsAsync(new TransferRequest
                    {
                        FromAccountId = senderId,
                        ToAccountId = receiverId,
                        Amount = amountPerTransfer,
                        Description = $"Paralel transfer #{index}"
                    }, $"concurrent-key-{index}");

                    return true;
                }
                catch (InsufficientFundsException)
                {
                    return false;
                }
            }));
        }

        barrier.Release(concurrentCount);

        var results = await Task.WhenAll(tasks);

        using (var verifyContext = new LedgerDbContext(options))
        {
            var senderEntries = await verifyContext.LedgerEntries
                .Where(l => l.AccountId == senderId)
                .ToListAsync();
            var finalSenderBalance = senderEntries.Sum(l => l.Amount);

            var receiverEntries = await verifyContext.LedgerEntries
                .Where(l => l.AccountId == receiverId)
                .ToListAsync();
            var finalReceiverBalance = receiverEntries.Sum(l => l.Amount);

            int successfulCount = results.Count(r => r);
            int failedCount = results.Count(r => !r);

            Assert.True(finalSenderBalance >= 0m, $"Balans mənfiyə düşməməlidir! Cari balans: {finalSenderBalance}");
            Assert.Equal(0m, finalSenderBalance);
            Assert.Equal(100m, finalReceiverBalance);
            Assert.Equal(5, successfulCount);
            Assert.Equal(5, failedCount);

            var totalDebitSum = senderEntries
                .Where(l => l.EntryType == EntryType.Debit)
                .Sum(l => l.Amount);

            Assert.Equal(-100m, totalDebitSum);
        }
    }
}

using AccountTransferLedgerService.Application.DTOs;
using AccountTransferLedgerService.Application.Services;
using AccountTransferLedgerService.Domain.Entities;
using AccountTransferLedgerService.Domain.Enums;
using AccountTransferLedgerService.Domain.Exceptions;
using AccountTransferLedgerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccountTransferLedgerService.Tests;

public class ConcurrentTransferTests
{
    [Fact]
    public async Task ConcurrentTransfers_DebitingSameAccount_NeverAllowsNegativeBalance()
    {
        // Arrange
        // Initial balance: 100 AZN
        // We will execute 10 concurrent requests of 20 AZN each (Total attempted: 200 AZN).
        // Under strict overdraft prevention, EXACTLY 5 transfers must succeed (5 * 20 = 100 AZN),
        // and 5 transfers must fail with InsufficientFundsException.
        // The final balance must be EXACTLY 0 AZN and NEVER negative!

        var dbName = "ConcurrentTestDb_" + Guid.NewGuid().ToString("N");
        
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        // Seed initial account and balance
        using (var setupContext = TestDbContextFactory.CreateInMemoryDbContext(dbName))
        {
            var sender = new Account { Id = senderId, AccountNumber = "AZ01SENDER", AccountHolderName = "Azər Məmmədov" };
            var receiver = new Account { Id = receiverId, AccountNumber = "AZ01RECEIVER", AccountHolderName = "Nigar Əliyeva" };

            await setupContext.Accounts.AddRangeAsync(sender, receiver);
            await setupContext.LedgerEntries.AddAsync(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = senderId,
                Amount = 100m, // 100 AZN initial balance
                EntryType = EntryType.Credit,
                Description = "İlkin balans"
            });
            await setupContext.SaveChangesAsync();
        }

        int concurrentCount = 10;
        decimal amountPerTransfer = 20m;
        var tasks = new List<Task<bool>>();
        var sharedAsyncLock = new AccountTransferLedgerService.Infrastructure.Concurrency.KeyedAsyncLock();

        // Semaphore / barrier to start all tasks simultaneously
        using var barrier = new SemaphoreSlim(0, concurrentCount);

        for (int i = 0; i < concurrentCount; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                // Wait for all tasks to be ready
                await barrier.WaitAsync();

                using var workerContext = TestDbContextFactory.CreateInMemoryDbContext(dbName);
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

                    return true; // Succeeded
                }
                catch (InsufficientFundsException)
                {
                    return false; // Safely blocked by overdraft prevention
                }
            }));
        }

        // Release all concurrent worker tasks at once
        barrier.Release(concurrentCount);

        // Wait for all concurrent transfer requests to finish
        var results = await Task.WhenAll(tasks);

        // Act & Assert
        using (var verifyContext = TestDbContextFactory.CreateInMemoryDbContext(dbName))
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

            // 1. Balance must NEVER be negative
            Assert.True(finalSenderBalance >= 0m, $"Balans mənfiyə düşməməlidir! Cari balans: {finalSenderBalance}");
            
            // 2. Exact balance verification
            Assert.Equal(0m, finalSenderBalance);
            Assert.Equal(100m, finalReceiverBalance);
            Assert.Equal(5, successfulCount);
            Assert.Equal(5, failedCount);

            // 3. Ledger consistency verification: Total debits = 100 AZN
            var totalDebitSum = senderEntries
                .Where(l => l.EntryType == EntryType.Debit)
                .Sum(l => l.Amount);

            Assert.Equal(-100m, totalDebitSum);
        }
    }
}

using AccountTransferLedger.Application.DTOs;
using AccountTransferLedger.Domain.Entities;
using AccountTransferLedger.Domain.Enums;
using AccountTransferLedger.Domain.Exceptions;
using AccountTransferLedger.Infrastructure.Concurrency;
using AccountTransferLedger.Infrastructure.Persistence;
using AccountTransferLedger.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccountTransferLedger.UnitTests;

public class InsufficientFundsTests
{
    [Fact]
    public async Task TransferFundsAsync_WhenAmountExceedsBalance_ThrowsInsufficientFundsExceptionAndRollsBack()
    {
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteDbContext();
        try
        {
            var dapperRepo = new DapperStatementRepository(dbContext);
            var idempotencyService = new IdempotencyService(dbContext);
            var asyncLock = new KeyedAsyncLock();
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
                Amount = 150m
            };

            var ex = await Assert.ThrowsAsync<InsufficientFundsException>(() =>
                service.TransferFundsAsync(request, "idemp-overdraft"));

            Assert.Equal("INSUFFICIENT_FUNDS", ex.ErrorCode);
            Assert.Equal(100m, ex.CurrentBalance);
            Assert.Equal(150m, ex.RequestedAmount);

            var transfersCount = await dbContext.Transfers.CountAsync();
            Assert.Equal(0, transfersCount);

            var senderBalance = await dapperRepo.GetCalculatedBalanceAsync(sender.Id);
            Assert.Equal(100m, senderBalance);

            var receiverBalance = await dapperRepo.GetCalculatedBalanceAsync(receiver.Id);
            Assert.Equal(0m, receiverBalance);
        }
        finally
        {
            connection.Dispose();
        }
    }
}

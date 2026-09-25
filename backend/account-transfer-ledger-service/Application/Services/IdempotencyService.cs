using AccountTransferLedgerService.Application.Interfaces;
using AccountTransferLedgerService.Domain.Entities;
using AccountTransferLedgerService.Domain.Enums;
using AccountTransferLedgerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountTransferLedgerService.Application.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly LedgerDbContext _dbContext;

    public IdempotencyService(LedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyRecord?> GetRecordAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        return await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);
    }

    public async Task<IdempotencyRecord> CreatePendingRecordAsync(
        string key, string path, string method, string bodyHash, CancellationToken cancellationToken = default)
    {
        var record = new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = key,
            RequestPath = path,
            RequestMethod = method,
            RequestBodyHash = bodyHash,
            Status = IdempotencyStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.IdempotencyRecords.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task MarkCompletedAsync(string key, int statusCode, string responseBody, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        if (record != null)
        {
            record.Status = IdempotencyStatus.Completed;
            record.ResponseStatusCode = statusCode;
            record.ResponseBody = responseBody;
            record.CompletedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkFailedAsync(string key, int statusCode, string responseBody, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        if (record != null)
        {
            record.Status = IdempotencyStatus.Failed;
            record.ResponseStatusCode = statusCode;
            record.ResponseBody = responseBody;
            record.CompletedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

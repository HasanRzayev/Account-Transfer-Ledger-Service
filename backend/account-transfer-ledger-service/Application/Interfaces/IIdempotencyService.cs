using AccountTransferLedgerService.Domain.Entities;

namespace AccountTransferLedgerService.Application.Interfaces;

public interface IIdempotencyService
{
    Task<IdempotencyRecord?> GetRecordAsync(string key, CancellationToken cancellationToken = default);
    Task<IdempotencyRecord> CreatePendingRecordAsync(string key, string path, string method, string bodyHash, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(string key, int statusCode, string responseBody, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(string key, int statusCode, string responseBody, CancellationToken cancellationToken = default);
}

using AccountTransferLedgerService.Application.DTOs;

namespace AccountTransferLedgerService.Application.Interfaces;

public interface ITransferService
{
    Task<TransferResultDto> TransferFundsAsync(TransferRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<TransferSummaryDto> GetTransferByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransferSummaryDto>> GetRecentTransfersAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<ConcurrencyStressTestResultDto> RunConcurrencyTestAsync(ConcurrencyStressTestRequest request, CancellationToken cancellationToken = default);
}

using AccountTransferLedger.Application.DTOs;

namespace AccountTransferLedger.Application.Interfaces;

public interface ITransferService
{
    Task<TransferResultDto> TransferFundsAsync(TransferRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<TransferSummaryDto> GetTransferByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransferSummaryDto>> GetRecentTransfersAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<ConcurrencyStressTestResultDto> RunConcurrencyTestAsync(ConcurrencyStressTestRequest request, CancellationToken cancellationToken = default);
}

public interface IStatementService
{
    Task<AccountStatementDto> GetAccountStatementAsync(Guid accountId, StatementQueryRequest request, CancellationToken cancellationToken = default);
}

public interface ILedgerStatementRepository
{
    Task<AccountStatementDto> GetStatementAsync(Guid accountId, StatementQueryRequest request, CancellationToken cancellationToken = default);
    Task<decimal> GetCalculatedBalanceAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetCalculatedBalancesAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default);
}

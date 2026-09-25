using AccountTransferLedgerService.Application.DTOs;

namespace AccountTransferLedgerService.Application.Interfaces;

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

using AccountTransferLedgerService.Application.DTOs;

namespace AccountTransferLedgerService.Application.Interfaces;

public interface IAccountService
{
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountDto>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<AccountDto> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountBalanceDto> GetAccountBalanceAsync(Guid id, CancellationToken cancellationToken = default);
}

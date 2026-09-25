using AccountTransferLedgerService.Application.DTOs;
using AccountTransferLedgerService.Application.Interfaces;
using AccountTransferLedgerService.Domain.Exceptions;
using AccountTransferLedgerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountTransferLedgerService.Application.Services;

public class StatementService : IStatementService
{
    private readonly ILedgerStatementRepository _statementRepository;
    private readonly LedgerDbContext _dbContext;

    public StatementService(ILedgerStatementRepository statementRepository, LedgerDbContext dbContext)
    {
        _statementRepository = statementRepository;
        _dbContext = dbContext;
    }

    public async Task<AccountStatementDto> GetAccountStatementAsync(
        Guid accountId, StatementQueryRequest request, CancellationToken cancellationToken = default)
    {
        var accountExists = await _dbContext.Accounts.AnyAsync(a => a.Id == accountId, cancellationToken);
        if (!accountExists)
        {
            throw new AccountNotFoundException(accountId);
        }

        return await _statementRepository.GetStatementAsync(accountId, request, cancellationToken);
    }
}

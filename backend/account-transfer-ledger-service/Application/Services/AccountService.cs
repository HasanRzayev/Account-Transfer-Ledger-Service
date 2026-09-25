using AccountTransferLedgerService.Application.DTOs;
using AccountTransferLedgerService.Application.Interfaces;
using AccountTransferLedgerService.Domain.Entities;
using AccountTransferLedgerService.Domain.Enums;
using AccountTransferLedgerService.Domain.Exceptions;
using AccountTransferLedgerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountTransferLedgerService.Application.Services;

public class AccountService : IAccountService
{
    private readonly LedgerDbContext _dbContext;
    private readonly ILedgerStatementRepository _statementRepository;

    public AccountService(LedgerDbContext dbContext, ILedgerStatementRepository statementRepository)
    {
        _dbContext = dbContext;
        _statementRepository = statementRepository;
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        // Generate a random unique account number: AZ + 2 check digits + ACNT + 6 random digits
        var random = new Random();
        string accountNumber;
        do
        {
            accountNumber = $"AZ88ACNT{random.Next(100000, 999999)}";
        } 
        while (await _dbContext.Accounts.AnyAsync(a => a.AccountNumber == accountNumber, cancellationToken));

        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            AccountHolderName = request.AccountHolderName.Trim(),
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "AZN" : request.Currency.ToUpper().Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _dbContext.Accounts.AddAsync(account, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (request.InitialBalance > 0)
            {
                var initialDeposit = new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = account.Id,
                    Amount = request.InitialBalance,
                    EntryType = EntryType.Credit,
                    Description = "İlkin açılış balansı depoziti",
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _dbContext.LedgerEntries.AddAsync(initialDeposit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var balance = request.InitialBalance;

        return new AccountDto
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            AccountHolderName = account.AccountHolderName,
            Currency = account.Currency,
            Balance = balance,
            IsActive = account.IsActive,
            CreatedAtUtc = account.CreatedAtUtc,
            TotalTransactionsCount = request.InitialBalance > 0 ? 1 : 0
        };
    }

    public async Task<IReadOnlyList<AccountDto>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
        {
            return Array.Empty<AccountDto>();
        }

        var balances = await _statementRepository.GetCalculatedBalancesAsync(
            accounts.Select(a => a.Id), cancellationToken
        );

        // Transaction counts
        var txCounts = await _dbContext.LedgerEntries
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AccountId, x => x.Count, cancellationToken);

        return accounts.Select(a => new AccountDto
        {
            Id = a.Id,
            AccountNumber = a.AccountNumber,
            AccountHolderName = a.AccountHolderName,
            Currency = a.Currency,
            Balance = balances.TryGetValue(a.Id, out var bal) ? bal : 0m,
            IsActive = a.IsActive,
            CreatedAtUtc = a.CreatedAtUtc,
            TotalTransactionsCount = txCounts.TryGetValue(a.Id, out var count) ? count : 0
        }).ToList();
    }

    public async Task<AccountDto> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account == null)
        {
            throw new AccountNotFoundException(id);
        }

        var balance = await _statementRepository.GetCalculatedBalanceAsync(id, cancellationToken);
        var txCount = await _dbContext.LedgerEntries.CountAsync(l => l.AccountId == id, cancellationToken);

        return new AccountDto
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            AccountHolderName = account.AccountHolderName,
            Currency = account.Currency,
            Balance = balance,
            IsActive = account.IsActive,
            CreatedAtUtc = account.CreatedAtUtc,
            TotalTransactionsCount = txCount
        };
    }

    public async Task<AccountBalanceDto> GetAccountBalanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account == null)
        {
            throw new AccountNotFoundException(id);
        }

        var balance = await _statementRepository.GetCalculatedBalanceAsync(id, cancellationToken);

        return new AccountBalanceDto
        {
            AccountId = account.Id,
            AccountNumber = account.AccountNumber,
            Balance = balance,
            Currency = account.Currency,
            CalculatedAtUtc = DateTime.UtcNow
        };
    }
}

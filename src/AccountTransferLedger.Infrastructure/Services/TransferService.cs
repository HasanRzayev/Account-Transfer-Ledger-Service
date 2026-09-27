using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AccountTransferLedger.Application.DTOs;
using AccountTransferLedger.Application.Interfaces;
using AccountTransferLedger.Domain.Entities;
using AccountTransferLedger.Domain.Enums;
using AccountTransferLedger.Domain.Exceptions;
using AccountTransferLedger.Infrastructure.Concurrency;
using AccountTransferLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AccountTransferLedger.Infrastructure.Services;

public class TransferService : ITransferService
{
    private readonly LedgerDbContext _dbContext;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILedgerStatementRepository _statementRepository;
    private readonly IKeyedAsyncLock _asyncLock;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ILogger<TransferService> _logger;

    public TransferService(
        LedgerDbContext dbContext,
        IIdempotencyService idempotencyService,
        ILedgerStatementRepository statementRepository,
        IKeyedAsyncLock asyncLock,
        ILogger<TransferService> logger)
        : this(dbContext, idempotencyService, statementRepository, asyncLock, null, logger)
    {
    }

    public TransferService(
        LedgerDbContext dbContext,
        IIdempotencyService idempotencyService,
        ILedgerStatementRepository statementRepository,
        IKeyedAsyncLock asyncLock,
        IServiceScopeFactory? scopeFactory,
        ILogger<TransferService> logger)
    {
        _dbContext = dbContext;
        _idempotencyService = idempotencyService;
        _statementRepository = statementRepository;
        _asyncLock = asyncLock;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<TransferResultDto> TransferFundsAsync(
        TransferRequest request, 
        string? idempotencyKey, 
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidTransferAmountException(request.Amount);
        }

        if (request.FromAccountId == request.ToAccountId)
        {
            throw new SelfTransferException();
        }

        string sanitizedKey = (idempotencyKey ?? string.Empty).Trim();
        string requestBodyHash = ComputeSha256(JsonSerializer.Serialize(request));

        if (!string.IsNullOrEmpty(sanitizedKey))
        {
            var existingRecord = await _idempotencyService.GetRecordAsync(sanitizedKey, cancellationToken);
            if (existingRecord != null)
            {
                if (existingRecord.Status == IdempotencyStatus.Completed)
                {
                    _logger.LogInformation("İdempotent köçürmə sorğusu təkrarlandı (Key: {Key}). Keşlənmiş cavab qaytarılır.", sanitizedKey);
                    var cached = JsonSerializer.Deserialize<TransferResultDto>(existingRecord.ResponseBody);
                    if (cached != null)
                    {
                        cached.WasCachedResponse = true;
                        return cached;
                    }
                }
                else if (existingRecord.Status == IdempotencyStatus.Pending)
                {
                    throw new IdempotencyConflictException(sanitizedKey);
                }
            }
            else
            {
                try
                {
                    await _idempotencyService.CreatePendingRecordAsync(
                        sanitizedKey, "/api/transfers", "POST", requestBodyHash, cancellationToken);
                }
                catch (Exception)
                {
                    var checkAgain = await _idempotencyService.GetRecordAsync(sanitizedKey, cancellationToken);
                    if (checkAgain?.Status == IdempotencyStatus.Completed)
                    {
                        var cached = JsonSerializer.Deserialize<TransferResultDto>(checkAgain.ResponseBody);
                        if (cached != null)
                        {
                            cached.WasCachedResponse = true;
                            return cached;
                        }
                    }
                    throw new IdempotencyConflictException(sanitizedKey);
                }
            }
        }

        using var asyncLockHandle = await _asyncLock.LockAsync(request.FromAccountId, request.ToAccountId, cancellationToken);

        var firstLockId = request.FromAccountId.CompareTo(request.ToAccountId) < 0 ? request.FromAccountId : request.ToAccountId;
        var secondLockId = request.FromAccountId.CompareTo(request.ToAccountId) < 0 ? request.ToAccountId : request.FromAccountId;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted, cancellationToken);

        TransferResultDto result;
        try
        {
            if (_dbContext.Database.IsRelational())
            {
                var isSqlServer = _dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) ?? false;
                var isPg = _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? false;

                if (isSqlServer)
                {
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        """
                        SELECT Id FROM Accounts WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
                        WHERE Id IN ({0}, {1})
                        ORDER BY Id;
                        """,
                        new object[] { firstLockId, secondLockId },
                        cancellationToken);
                }
                else if (isPg)
                {
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        """
                        SELECT "Id" FROM "Accounts" 
                        WHERE "Id" IN ({0}, {1}) 
                        ORDER BY "Id" 
                        FOR UPDATE;
                        """, 
                        new object[] { firstLockId, secondLockId }, 
                        cancellationToken);
                }
                else
                {
                    await _dbContext.Accounts
                        .Where(a => a.Id == firstLockId || a.Id == secondLockId)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.RowVersion, a => a.RowVersion + 1), cancellationToken);
                }
            }

            var fromAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == request.FromAccountId, cancellationToken);
            if (fromAccount == null)
            {
                throw new AccountNotFoundException(request.FromAccountId);
            }

            var toAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == request.ToAccountId, cancellationToken);
            if (toAccount == null)
            {
                throw new AccountNotFoundException(request.ToAccountId);
            }

            if (!fromAccount.IsActive)
            {
                throw new DomainException($"Göndərən hesab qeyri-aktivdir: {fromAccount.AccountNumber}");
            }

            if (!toAccount.IsActive)
            {
                throw new DomainException($"Alan hesab qeyri-aktivdir: {toAccount.AccountNumber}");
            }

            var currentSourceBalance = await _statementRepository.GetCalculatedBalanceAsync(request.FromAccountId, cancellationToken);

            if (currentSourceBalance < request.Amount)
            {
                throw new InsufficientFundsException(fromAccount.Id, currentSourceBalance, request.Amount);
            }

            var currentDestBalance = await _statementRepository.GetCalculatedBalanceAsync(request.ToAccountId, cancellationToken);

            var transfer = new Transfer
            {
                Id = Guid.NewGuid(),
                FromAccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Amount = request.Amount,
                Currency = fromAccount.Currency,
                Description = string.IsNullOrWhiteSpace(request.Description) ? "Hesablararası köçürmə" : request.Description.Trim(),
                IdempotencyKey = sanitizedKey,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _dbContext.Transfers.AddAsync(transfer, cancellationToken);

            var debitEntry = new LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = fromAccount.Id,
                TransferId = transfer.Id,
                Amount = -request.Amount,
                EntryType = EntryType.Debit,
                Description = $"Köçürmə çıxışı -> {toAccount.AccountNumber} ({toAccount.AccountHolderName})",
                CreatedAtUtc = transfer.CreatedAtUtc
            };

            var creditEntry = new LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = toAccount.Id,
                TransferId = transfer.Id,
                Amount = request.Amount,
                EntryType = EntryType.Credit,
                Description = $"Köçürmə mədaxili <- {fromAccount.AccountNumber} ({fromAccount.AccountHolderName})",
                CreatedAtUtc = transfer.CreatedAtUtc
            };

            await _dbContext.LedgerEntries.AddRangeAsync(new[] { debitEntry, creditEntry }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var sourceNewBalance = currentSourceBalance - request.Amount;
            var destNewBalance = currentDestBalance + request.Amount;

            result = new TransferResultDto
            {
                TransferId = transfer.Id,
                FromAccountId = fromAccount.Id,
                FromAccountNumber = fromAccount.AccountNumber,
                FromAccountHolder = fromAccount.AccountHolderName,
                ToAccountId = toAccount.Id,
                ToAccountNumber = toAccount.AccountNumber,
                ToAccountHolder = toAccount.AccountHolderName,
                Amount = transfer.Amount,
                Currency = transfer.Currency,
                Description = transfer.Description,
                IdempotencyKey = sanitizedKey,
                SourceNewBalance = sourceNewBalance,
                DestinationNewBalance = destNewBalance,
                CreatedAtUtc = transfer.CreatedAtUtc,
                WasCachedResponse = false
            };

            if (!string.IsNullOrEmpty(sanitizedKey))
            {
                await _idempotencyService.MarkCompletedAsync(
                    sanitizedKey, 200, JsonSerializer.Serialize(result), cancellationToken);
            }

            _logger.LogInformation(
                "Köçürmə uğurla tamamlandı: {Amount} {Currency} ({From} -> {To})", 
                result.Amount, result.Currency, result.FromAccountNumber, result.ToAccountNumber);

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            if (!string.IsNullOrEmpty(sanitizedKey))
            {
                await _idempotencyService.MarkFailedAsync(
                    sanitizedKey, 
                    ex is DomainException de ? de.StatusCode : 500, 
                    JsonSerializer.Serialize(new { error = ex.Message }), 
                    cancellationToken);
            }

            throw;
        }
    }

    public async Task<TransferSummaryDto> GetTransferByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _dbContext.Transfers
            .AsNoTracking()
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transfer == null)
        {
            throw new DomainException($"Köçürmə tapılmadı: ID {id}");
        }

        return new TransferSummaryDto
        {
            Id = transfer.Id,
            FromAccountId = transfer.FromAccountId,
            FromAccountNumber = transfer.FromAccount?.AccountNumber ?? string.Empty,
            FromAccountHolder = transfer.FromAccount?.AccountHolderName ?? string.Empty,
            ToAccountId = transfer.ToAccountId,
            ToAccountNumber = transfer.ToAccount?.AccountNumber ?? string.Empty,
            ToAccountHolder = transfer.ToAccount?.AccountHolderName ?? string.Empty,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            Description = transfer.Description,
            IdempotencyKey = transfer.IdempotencyKey,
            CreatedAtUtc = transfer.CreatedAtUtc
        };
    }

    public async Task<IReadOnlyList<TransferSummaryDto>> GetRecentTransfersAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var transfers = await _dbContext.Transfers
            .AsNoTracking()
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return transfers.Select(t => new TransferSummaryDto
        {
            Id = t.Id,
            FromAccountId = t.FromAccountId,
            FromAccountNumber = t.FromAccount?.AccountNumber ?? string.Empty,
            FromAccountHolder = t.FromAccount?.AccountHolderName ?? string.Empty,
            ToAccountId = t.ToAccountId,
            ToAccountNumber = t.ToAccount?.AccountNumber ?? string.Empty,
            ToAccountHolder = t.ToAccount?.AccountHolderName ?? string.Empty,
            Amount = t.Amount,
            Currency = t.Currency,
            Description = t.Description,
            IdempotencyKey = t.IdempotencyKey,
            CreatedAtUtc = t.CreatedAtUtc
        }).ToList();
    }

    public async Task<ConcurrencyStressTestResultDto> RunConcurrencyTestAsync(
        ConcurrencyStressTestRequest request, CancellationToken cancellationToken = default)
    {
        var initialSourceBal = await _statementRepository.GetCalculatedBalanceAsync(request.SourceAccountId, cancellationToken);

        var tasks = new List<Task<StressTestDetailItem>>();
        for (int i = 1; i <= request.ConcurrentRequestsCount; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var scope = _scopeFactory?.CreateScope();
                    var transferService = scope != null 
                        ? scope.ServiceProvider.GetRequiredService<ITransferService>()
                        : this;

                    var res = await transferService.TransferFundsAsync(new TransferRequest
                    {
                        FromAccountId = request.SourceAccountId,
                        ToAccountId = request.DestinationAccountId,
                        Amount = request.TransferAmountPerRequest,
                        Description = $"Konkurent test köçürməsi #{index}"
                    }, Guid.NewGuid().ToString("N"), cancellationToken);

                    sw.Stop();
                    return new StressTestDetailItem
                    {
                        Index = index,
                        Success = true,
                        StatusCode = 200,
                        Message = $"Uğurlu köçürmə: {request.TransferAmountPerRequest:N2} AZN",
                        DurationMs = sw.ElapsedMilliseconds
                    };
                }
                catch (InsufficientFundsException ex)
                {
                    sw.Stop();
                    return new StressTestDetailItem
                    {
                        Index = index,
                        Success = false,
                        StatusCode = 422,
                        ErrorCode = ex.ErrorCode,
                        Message = ex.Message,
                        DurationMs = sw.ElapsedMilliseconds
                    };
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    var msg = ex is DomainException de ? de.Message : "Sistemdə gözlənilməz xəta baş verdi.";
                    return new StressTestDetailItem
                    {
                        Index = index,
                        Success = false,
                        StatusCode = ex is DomainException de2 ? de2.StatusCode : 500,
                        ErrorCode = ex is DomainException de3 ? de3.ErrorCode : "ERROR",
                        Message = msg,
                        DurationMs = sw.ElapsedMilliseconds
                    };
                }
            }, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        var finalSourceBal = await _statementRepository.GetCalculatedBalanceAsync(request.SourceAccountId, cancellationToken);
        var finalDestBal = await _statementRepository.GetCalculatedBalanceAsync(request.DestinationAccountId, cancellationToken);

        int successfulCount = results.Count(r => r.Success);
        int failedCount = results.Count(r => !r.Success);
        bool overdraftPrevented = finalSourceBal >= 0;

        return new ConcurrencyStressTestResultDto
        {
            TotalRequests = request.ConcurrentRequestsCount,
            SuccessfulRequests = successfulCount,
            FailedRequests = failedCount,
            InitialSourceBalance = initialSourceBal,
            FinalSourceBalance = finalSourceBal,
            FinalDestinationBalance = finalDestBal,
            OverdraftPrevented = overdraftPrevented,
            SummaryMessage = overdraftPrevented 
                ? $"Konkurentlik testi tamamlandı: {request.ConcurrentRequestsCount} paralel sorğudan {successfulCount}-i uğurla icra olundu, {failedCount}-i vəsait çatışmazlığına görə təhlükəsiz dayandırıldı. Balans heç vaxt mənfiyə düşmədi (Son balans: {finalSourceBal:N2} AZN)."
                : "XƏTA: Balans mənfiyə düşdü!",
            Details = results.OrderBy(r => r.Index).ToList()
        };
    }

    private static string ComputeSha256(string rawData)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes);
    }
}

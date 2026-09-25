using AccountTransferLedgerService.Domain.Enums;

namespace AccountTransferLedgerService.Application.DTOs;

public class StatementQueryRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class LedgerEntryDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? TransferId { get; set; }
    public decimal Amount { get; set; }
    public EntryType EntryType { get; set; }
    public string EntryTypeName => EntryType == EntryType.Credit ? "Kredit (+)" : "Debet (-)";
    public string Description { get; set; } = string.Empty;
    public decimal RunningBalance { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CounterpartyAccountNumber { get; set; }
    public string? CounterpartyHolderName { get; set; }
}

public class AccountStatementDto
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string Currency { get; set; } = "AZN";
    public decimal CurrentBalance { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
    public IReadOnlyList<LedgerEntryDto> Entries { get; set; } = new List<LedgerEntryDto>();
}

using AccountTransferLedger.Domain.Enums;

namespace AccountTransferLedger.Domain.Entities;

public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    
    public Guid? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    
    public decimal Amount { get; set; }
    
    public EntryType EntryType { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

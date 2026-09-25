namespace AccountTransferLedger.Domain.Entities;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string AccountNumber { get; set; } = string.Empty;
    
    public string AccountHolderName { get; set; } = string.Empty;
    
    public string Currency { get; set; } = "AZN";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public long RowVersion { get; set; } = 1;

    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}

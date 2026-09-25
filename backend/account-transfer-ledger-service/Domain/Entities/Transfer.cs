namespace AccountTransferLedgerService.Domain.Entities;

/// <summary>
/// İki hesab arasındakı köçürmə əməliyyatı.
/// Hər bir köçürmə atomik olaraq 2 ədəd LedgerEntry yaradır (1 Debet, 1 Kredit).
/// </summary>
public class Transfer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid FromAccountId { get; set; }
    public Account? FromAccount { get; set; }
    
    public Guid ToAccountId { get; set; }
    public Account? ToAccount { get; set; }
    
    public decimal Amount { get; set; }
    
    public string Currency { get; set; } = "AZN";
    
    public string Description { get; set; } = string.Empty;
    
    public string IdempotencyKey { get; set; } = string.Empty;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}

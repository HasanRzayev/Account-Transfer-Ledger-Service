using AccountTransferLedgerService.Domain.Enums;

namespace AccountTransferLedgerService.Domain.Entities;

/// <summary>
/// İkiqat mühasibatlıq (Double-Entry) Baş Kitab Qeydi.
/// Dəyişdirilməzdir (Immutable). Balans bu qeydlərin cəmindən ibarətdir: SUM(Amount).
/// </summary>
public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    
    public Guid? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    
    /// <summary>
    /// İşarələnmiş məbləğ: Kredit üçün müsbət (+), Debet üçün mənfi (-).
    /// </summary>
    public decimal Amount { get; set; }
    
    public EntryType EntryType { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

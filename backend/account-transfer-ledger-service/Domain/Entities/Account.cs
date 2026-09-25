namespace AccountTransferLedgerService.Domain.Entities;

/// <summary>
/// Bank Hesabı Varlığı.
/// QEYD: Tələbə əsasən, balans bu cədvəldə saxlanılmır (mutable column yoxdur).
/// Bütün balanslar Baş Kitab (LedgerEntry) qeydlərinin cəmindən dinamik hesablanır.
/// </summary>
public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string AccountNumber { get; set; } = string.Empty;
    
    public string AccountHolderName { get; set; } = string.Empty;
    
    public string Currency { get; set; } = "AZN";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Optimistik və ya paralel kilidləmə üçün ardıcıllıq versiyası
    public long RowVersion { get; set; } = 1;

    // Əlaqəli Baş Kitab qeydləri
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}

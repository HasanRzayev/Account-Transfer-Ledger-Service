using AccountTransferLedgerService.Domain.Enums;

namespace AccountTransferLedgerService.Domain.Entities;

/// <summary>
/// Idempotency-Key qeydi.
/// Eyni sorğu təkrarlandıqda təkrar icranın qarşısını alır və saxlanılmış cavabı qaytarır.
/// </summary>
public class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Key { get; set; } = string.Empty;
    
    public string RequestPath { get; set; } = string.Empty;
    
    public string RequestMethod { get; set; } = string.Empty;
    
    public string RequestBodyHash { get; set; } = string.Empty;
    
    public int ResponseStatusCode { get; set; }
    
    public string ResponseBody { get; set; } = string.Empty;
    
    public IdempotencyStatus Status { get; set; } = IdempotencyStatus.Pending;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAtUtc { get; set; }
}

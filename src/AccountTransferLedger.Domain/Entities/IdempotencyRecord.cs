using AccountTransferLedger.Domain.Enums;

namespace AccountTransferLedger.Domain.Entities;

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

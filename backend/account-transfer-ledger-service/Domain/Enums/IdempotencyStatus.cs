namespace AccountTransferLedgerService.Domain.Enums;

/// <summary>
/// İdempotentlik əməliyyatının vəziyyəti
/// </summary>
public enum IdempotencyStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}

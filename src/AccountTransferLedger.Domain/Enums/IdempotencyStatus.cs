namespace AccountTransferLedger.Domain.Enums;

public enum IdempotencyStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}

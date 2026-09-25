namespace AccountTransferLedgerService.Domain.Exceptions;

public class DomainException : Exception
{
    public virtual string ErrorCode { get; } = "DOMAIN_ERROR";
    public virtual int StatusCode { get; } = 400;

    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, string errorCode, int statusCode = 400) : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class AccountNotFoundException : DomainException
{
    public override string ErrorCode => "ACCOUNT_NOT_FOUND";
    public override int StatusCode => 404;

    public AccountNotFoundException(Guid accountId) 
        : base($"Hesab tapılmadı (ID: {accountId}).")
    {
    }

    public AccountNotFoundException(string identifier) 
        : base($"Hesab tapılmadı: {identifier}.")
    {
    }
}

public class InsufficientFundsException : DomainException
{
    public override string ErrorCode => "INSUFFICIENT_FUNDS";
    public override int StatusCode => 422;

    public Guid AccountId { get; }
    public decimal CurrentBalance { get; }
    public decimal RequestedAmount { get; }

    public InsufficientFundsException(Guid accountId, decimal currentBalance, decimal requestedAmount) 
        : base($"Hesabda kifayət qədər vəsait yoxdur. Mövcud balans: {currentBalance:N2} AZN, Tələb olunan məbləğ: {requestedAmount:N2} AZN.")
    {
        AccountId = accountId;
        CurrentBalance = currentBalance;
        RequestedAmount = requestedAmount;
    }
}

public class InvalidTransferAmountException : DomainException
{
    public override string ErrorCode => "INVALID_TRANSFER_AMOUNT";
    public override int StatusCode => 400;

    public InvalidTransferAmountException(decimal amount) 
        : base($"Köçürmə məbləği müsbət olmalıdır. Göndərilən məbləğ: {amount:N2}.")
    {
    }
}

public class SelfTransferException : DomainException
{
    public override string ErrorCode => "SELF_TRANSFER_NOT_ALLOWED";
    public override int StatusCode => 400;

    public SelfTransferException() 
        : base("Eyni hesab daxilində köçürmə icra edilə bilməz. Göndərən və alan hesab fərqli olmalıdır.")
    {
    }
}

public class IdempotencyConflictException : DomainException
{
    public override string ErrorCode => "IDEMPOTENCY_CONFLICT";
    public override int StatusCode => 409;

    public IdempotencyConflictException(string idempotencyKey) 
        : base($"Bu Idempotency-Key ('{idempotencyKey}') ilə başqa bir əməliyyat hazırda icra olunur və ya parametrlər uyğunsuzdur.")
    {
    }
}

public class DuplicateAccountException : DomainException
{
    public override string ErrorCode => "DUPLICATE_ACCOUNT";
    public override int StatusCode => 409;

    public DuplicateAccountException(string accountNumber) 
        : base($"Bu hesab nömrəsi artıq mövcuddur: {accountNumber}.")
    {
    }
}

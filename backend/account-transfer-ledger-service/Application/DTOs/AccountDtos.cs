using System.ComponentModel.DataAnnotations;

namespace AccountTransferLedgerService.Application.DTOs;

public class CreateAccountRequest
{
    [Required(ErrorMessage = "Hesab sahibinin adı mütləqdir.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Hesab sahibinin adı 2-150 simvol arasında olmalıdır.")]
    public string AccountHolderName { get; set; } = string.Empty;

    [Range(0, 1000000000, ErrorMessage = "İlkin balans 0 və ya daha böyük olmalıdır.")]
    public decimal InitialBalance { get; set; } = 0;

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Valyuta 3 hərfli ISO kod olmalıdır (məsələn: AZN).")]
    public string Currency { get; set; } = "AZN";
}

public class AccountDto
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int TotalTransactionsCount { get; set; }
}

public class AccountBalanceDto
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
}

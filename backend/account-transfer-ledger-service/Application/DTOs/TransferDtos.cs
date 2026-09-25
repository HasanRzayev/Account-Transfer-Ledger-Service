using System.ComponentModel.DataAnnotations;

namespace AccountTransferLedgerService.Application.DTOs;

public class TransferRequest
{
    [Required(ErrorMessage = "Göndərən hesabın ID-si mütləqdir.")]
    public Guid FromAccountId { get; set; }

    [Required(ErrorMessage = "Alan hesabın ID-si mütləqdir.")]
    public Guid ToAccountId { get; set; }

    [Required(ErrorMessage = "Köçürmə məbləği mütləqdir.")]
    [Range(0.01, 1000000000, ErrorMessage = "Köçürmə məbləği 0.01-dən böyük olmalıdır.")]
    public decimal Amount { get; set; }

    [StringLength(250, ErrorMessage = "Təyinat mətni maksimum 250 simvol ola bilər.")]
    public string Description { get; set; } = string.Empty;
}

public class TransferResultDto
{
    public Guid TransferId { get; set; }
    public Guid FromAccountId { get; set; }
    public string FromAccountNumber { get; set; } = string.Empty;
    public string FromAccountHolder { get; set; } = string.Empty;
    public Guid ToAccountId { get; set; }
    public string ToAccountNumber { get; set; } = string.Empty;
    public string ToAccountHolder { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN";
    public string Description { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public decimal SourceNewBalance { get; set; }
    public decimal DestinationNewBalance { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool WasCachedResponse { get; set; }
}

public class TransferSummaryDto
{
    public Guid Id { get; set; }
    public Guid FromAccountId { get; set; }
    public string FromAccountNumber { get; set; } = string.Empty;
    public string FromAccountHolder { get; set; } = string.Empty;
    public Guid ToAccountId { get; set; }
    public string ToAccountNumber { get; set; } = string.Empty;
    public string ToAccountHolder { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN";
    public string Description { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

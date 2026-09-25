namespace AccountTransferLedgerService.Application.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public ApiErrorDetails? Error { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Əməliyyat uğurla tamamlandı.")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string errorCode, string message, int statusCode, object? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Error = new ApiErrorDetails
            {
                Code = errorCode,
                Message = message,
                StatusCode = statusCode,
                Details = details,
                TimestampUtc = DateTime.UtcNow
            }
        };
    }
}

public class ApiErrorDetails
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public object? Details { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class ConcurrencyStressTestRequest
{
    public Guid SourceAccountId { get; set; }
    public Guid DestinationAccountId { get; set; }
    public decimal TransferAmountPerRequest { get; set; } = 20;
    public int ConcurrentRequestsCount { get; set; } = 10;
}

public class ConcurrencyStressTestResultDto
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public decimal InitialSourceBalance { get; set; }
    public decimal FinalSourceBalance { get; set; }
    public decimal FinalDestinationBalance { get; set; }
    public bool OverdraftPrevented { get; set; }
    public string SummaryMessage { get; set; } = string.Empty;
    public List<StressTestDetailItem> Details { get; set; } = new();
}

public class StressTestDetailItem
{
    public int Index { get; set; }
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public long DurationMs { get; set; }
}

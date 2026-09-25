using System.Net;
using System.Text.Json;
using AccountTransferLedger.Application.DTOs;
using AccountTransferLedger.Domain.Exceptions;

namespace AccountTransferLedger.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        string errorCode;
        string message;
        object? details = null;

        switch (exception)
        {
            case InsufficientFundsException ife:
                statusCode = ife.StatusCode;
                errorCode = ife.ErrorCode;
                message = ife.Message;
                details = new 
                { 
                    accountId = ife.AccountId, 
                    currentBalance = ife.CurrentBalance, 
                    requestedAmount = ife.RequestedAmount 
                };
                _logger.LogWarning("Vəsait çatışmazlığı: {Message}", message);
                break;

            case DomainException de:
                statusCode = de.StatusCode;
                errorCode = de.ErrorCode;
                message = de.Message;
                _logger.LogWarning("Domen xətası ({Code}): {Message}", errorCode, message);
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                errorCode = "INTERNAL_SERVER_ERROR";
                message = "Sistemdə gözlənilməz daxili xəta baş verdi. Zəhmət olmasa bir az sonra yenidən cəhd edin.";
                _logger.LogError(exception, "Gözlənilməz server xətası baş verdi.");
                break;
        }

        var response = ApiResponse<object>.Fail(errorCode, message, statusCode, details);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

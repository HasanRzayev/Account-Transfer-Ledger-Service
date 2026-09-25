using AccountTransferLedgerService.Application.DTOs;
using AccountTransferLedgerService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AccountTransferLedgerService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;

    public TransfersController(ITransferService transferService)
    {
        _transferService = transferService;
    }

    /// <summary>
    /// İki hesab arasında atomik pul köçürməsi icra edir.
    /// Idempotency-Key başlığı vasitəsilə təkrar sorğuların iki dəfə icrasının qarşısı alınır.
    /// </summary>
    /// <param name="request">Köçürmə parametrləri (Mənbə hesab, Hədəf hesab, Məbləğ, Təyinat)</param>
    /// <param name="idempotencyKey">Təkrar icranın qarşısını alan unikal açar (UUID və ya unikal sətir)</param>
    /// <param name="cancellationToken">Ləğv tokeni</param>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransferResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> TransferFunds(
        [FromBody] TransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", errors, StatusCodes.Status400BadRequest));
        }

        var result = await _transferService.TransferFundsAsync(request, idempotencyKey, cancellationToken);
        
        string message = result.WasCachedResponse 
            ? "Sorğu əvvəllər icra olunub (İdempotent keşlənmiş cavab qaytarıldı)."
            : "Köçürmə uğurla tamamlandı.";

        return Ok(ApiResponse<TransferResultDto>.Ok(result, message));
    }

    /// <summary>
    /// ID-yə görə köçürmənin detallarını qaytarır.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TransferSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransferById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _transferService.GetTransferByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<TransferSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Son köçürmələrin siyahısını qaytarır.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TransferSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentTransfers([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var result = await _transferService.GetRecentTransfersAsync(limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TransferSummaryDto>>.Ok(result, "Son köçürmələr əldə edildi."));
    }
}

using AccountTransferLedgerService.Application.DTOs;
using AccountTransferLedgerService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AccountTransferLedgerService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatementsController : ControllerBase
{
    private readonly IStatementService _statementService;

    public StatementsController(IStatementService statementService)
    {
        _statementService = statementService;
    }

    /// <summary>
    /// Verilmiş hesab üçün Dapper ilə optimizasiya edilmiş səhifələnmiş Baş Kitab çıxarışı (Hesab Çıxarışı).
    /// Hər sətirdə Debet (-)/Kredit (+) və cari qalıq (running balance) hesablanır.
    /// </summary>
    [HttpGet("{accountId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AccountStatementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatement(
        Guid accountId, 
        [FromQuery] StatementQueryRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await _statementService.GetAccountStatementAsync(accountId, request, cancellationToken);
        return Ok(ApiResponse<AccountStatementDto>.Ok(result, "Hesab çıxarışı uğurla formalaşdırıldı."));
    }
}

using AccountTransferLedger.Application.DTOs;
using AccountTransferLedger.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AccountTransferLedger.API.Controllers;

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

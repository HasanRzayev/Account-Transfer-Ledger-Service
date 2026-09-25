using AccountTransferLedger.Application.DTOs;
using AccountTransferLedger.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AccountTransferLedger.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", errors, StatusCodes.Status400BadRequest));
        }

        var result = await _accountService.CreateAccountAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAccountById), new { id = result.Id }, ApiResponse<AccountDto>.Ok(result, "Hesab uğurla yaradıldı."));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AccountDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAccounts(CancellationToken cancellationToken)
    {
        var result = await _accountService.GetAllAccountsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccountDto>>.Ok(result, "Hesablar siyahısı uğurla əldə edildi."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _accountService.GetAccountByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<AccountDto>.Ok(result));
    }

    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType(typeof(ApiResponse<AccountBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountBalance(Guid id, CancellationToken cancellationToken)
    {
        var result = await _accountService.GetAccountBalanceAsync(id, cancellationToken);
        return Ok(ApiResponse<AccountBalanceDto>.Ok(result, "Balans uğurla hesablandı."));
    }
}

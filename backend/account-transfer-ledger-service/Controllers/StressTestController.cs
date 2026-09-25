using AccountTransferLedgerService.Application.DTOs;
using AccountTransferLedgerService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AccountTransferLedgerService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StressTestController : ControllerBase
{
    private readonly ITransferService _transferService;

    public StressTestController(ITransferService transferService)
    {
        _transferService = transferService;
    }

    /// <summary>
    /// Eyni hesaba eyni anda çoxsaylı paralel köçürmə göndərərək overdraft qarşısının alınmasını və balansın mənfiyə düşməməsini yoxlayır.
    /// </summary>
    [HttpPost("concurrency")]
    [ProducesResponseType(typeof(ApiResponse<ConcurrencyStressTestResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunConcurrencyStressTest(
        [FromBody] ConcurrencyStressTestRequest request, 
        CancellationToken cancellationToken)
    {
        if (request.ConcurrentRequestsCount < 1 || request.ConcurrentRequestsCount > 100)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST_COUNT", "Paralel sorğu sayı 1 ilə 100 arasında olmalıdır.", 400));
        }

        var result = await _transferService.RunConcurrencyTestAsync(request, cancellationToken);
        return Ok(ApiResponse<ConcurrencyStressTestResultDto>.Ok(result, result.SummaryMessage));
    }
}

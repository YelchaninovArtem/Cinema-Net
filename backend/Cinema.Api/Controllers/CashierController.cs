using Cinema.Application.Cashier;
using Cinema.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/cashier")]
[Authorize(Roles = "Cashier,Admin")]
public sealed class CashierController : ControllerBase
{
    private readonly ICashierService _svc;
    public CashierController(ICashierService svc) => _svc = svc;

    [HttpGet("ticket/verify")]
    public async Task<IActionResult> Verify([FromQuery] string qr, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(qr))
            return BadRequest(new { error = "QR token is required." });
        var result = await _svc.VerifyByQrAsync(qr, ct);
        if (result is null) return NotFound(new { error = "Ticket not found." });
        return Ok(result);
    }

    [HttpGet("tickets/{id:int}")]
    public async Task<IActionResult> GetTicket(int id, CancellationToken ct)
    {
        var result = await _svc.VerifyByIdAsync(id, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost("tickets/{id:int}/use")]
    public async Task<IActionResult> UseTicket(int id, CancellationToken ct)
    {
        try
        {
            var result = await _svc.UseTicketAsync(id, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("offline-sale")]
    public async Task<IActionResult> OfflineSale([FromBody] OfflineSaleRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.GuestEmail))
            return BadRequest(new { error = "Buyer email is required." });

        try
        {
            var result = await _svc.CreateOfflineSaleAsync(request, ct);
            if (result is null) return Conflict(new { error = "One or more seats are already taken." });
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("tickets/{id:int}/refund")]
    public async Task<IActionResult> Refund(int id, CancellationToken ct)
    {
        try
        {
            var result = await _svc.RefundTicketAsync(id, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }
}

using Cinema.Application.Account;
using Cinema.Application.Tickets;
using Cinema.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly IAccountService _accountService;

    public TicketsController(ITicketService ticketService, IAccountService accountService)
    {
        _ticketService = ticketService;
        _accountService = accountService;
    }

    /// <summary>Купити квитки (instant buy). Авторизація необов'язкова — гість передає GuestEmail.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateTickets([FromBody] CreateTicketsRequest req, CancellationToken ct)
    {
        if (User.IsInRole("Cashier"))
            return BadRequest(new { error = "Cashier accounts cannot buy tickets online. Use the Cashier Panel offline sale instead." });

        var userId = User.Identity?.IsAuthenticated == true
            ? User.GetUserId()
            : null;

        if (userId is null && string.IsNullOrWhiteSpace(req.GuestEmail))
            return BadRequest(new { error = "GuestEmail required for unauthenticated purchases." });

        try
        {
            var result = await _ticketService.CreateTicketsAsync(req, userId, ct);
            if (result is null)
                return Conflict(new { error = "One or more seats are already taken." });
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Отримати квиток за ID (тільки для власника).</summary>
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetTicket(int id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _accountService.GetTicketDetailAsync(id, userId, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>Отримати власні квитки.</summary>
    [HttpGet("my-tickets")]
    [Authorize]
    public async Task<IActionResult> GetMyTickets(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tickets = await _ticketService.GetUserTicketsAsync(userId, ct);
        return Ok(tickets);
    }

    /// <summary>Отримати QR-код квитка (тільки для власника).</summary>
    [HttpGet("{id:int}/qr")]
    [Authorize]
    public async Task<IActionResult> GetQrCode(int id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        try
        {
            var stream = await _accountService.GetTicketQrAsync(id, userId, ct);
            return File(stream, "image/png");
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}

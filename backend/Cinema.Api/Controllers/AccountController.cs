using System.Security.Claims;
using Cinema.Application.Account;
using Cinema.Application.Loyalty;
using Cinema.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController : ControllerBase
{
    private readonly IAccountService _svc;
    private readonly ILoyaltyService _loyalty;

    public AccountController(IAccountService svc, ILoyaltyService loyalty)
    {
        _svc     = svc;
        _loyalty = loyalty;
    }

    /// <summary>Список квитків авторизованого користувача.</summary>
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tickets = await _svc.GetUserTicketsAsync(userId, ct);
        return Ok(tickets);
    }

    [HttpPost("tickets/{id:int}/refund")]
    public async Task<IActionResult> RefundTicket(int id, CancellationToken ct)
    {
        var userId = User.GetUserId();

        try
        {
            return Ok(await _svc.RefundTicketAsync(id, userId, ct));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // ── Обране ──────────────────────────────────────────────────────────────

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites(CancellationToken ct)
    {
        var userId = User.GetUserId();
        return Ok(await _svc.GetFavoritesAsync(userId, ct));
    }

    [HttpPost("favorites/{movieId:int}")]
    public async Task<IActionResult> AddFavorite(int movieId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _svc.AddFavoriteAsync(userId, movieId, ct);
        return NoContent();
    }

    [HttpDelete("favorites/{movieId:int}")]
    public async Task<IActionResult> RemoveFavorite(int movieId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _svc.RemoveFavoriteAsync(userId, movieId, ct);
        return NoContent();
    }

    // ── Лояльність ──────────────────────────────────────────────────────────

    [HttpGet("loyalty/balance")]
    public async Task<IActionResult> GetLoyaltyBalance(CancellationToken ct)
    {
        var userId = User.GetUserId();
        return Ok(await _loyalty.GetBalanceAsync(userId, ct));
    }

    // Loyalty apply/cancel endpoints removed – applied during purchase
}

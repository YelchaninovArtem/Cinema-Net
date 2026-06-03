using Cinema.Application.PromoCodes;
using Cinema.Domain.Common;
using Cinema.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class PromoCodesController(IPromoCodeService promos, CinemaDbContext db) : ControllerBase
{
    // ── Адмінські endpoints ────────────────────────────────────────────────

    [HttpGet("admin/promo-codes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await promos.GetAllAsync(ct));

    [HttpGet("admin/promo-codes/{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PromoCodeDto), 200)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var promo = await promos.GetByIdAsync(id, ct);
        return promo is null ? NotFound() : Ok(promo);
    }

    [HttpPost("admin/promo-codes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreatePromoCodeRequest request,
        CancellationToken ct)
    {
        try
        {
            var dto = await promos.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("admin/promo-codes/{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdatePromoCodeRequest request,
        CancellationToken ct)
    {
        try
        {
            await promos.UpdateAsync(id, request, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { error = ex.Message })
                : BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("admin/promo-codes/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await promos.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("admin/users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await db.Users
            .Select(u => new UserDto(u.Id, u.Email ?? string.Empty, u.FirstName, u.LastName))
            .ToListAsync(ct);
        return Ok(users);
    }
}

public sealed record UserDto(string Id, string Email, string FirstName, string LastName);

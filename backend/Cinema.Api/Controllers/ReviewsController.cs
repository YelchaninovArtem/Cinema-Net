using System.Security.Claims;
using Cinema.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _svc;

    public ReviewsController(IReviewService svc) => _svc = svc;

    /// <summary>Усі відгуки для фільму + середній рейтинг.</summary>
    [HttpGet("movies/{movieId:int}")]
    public async Task<IActionResult> GetForMovie(int movieId, CancellationToken ct) =>
        Ok(await _svc.GetMovieReviewsAsync(movieId, ct));

    /// <summary>Усі відгуки поточного користувача.</summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = User.GetUserId();
        return Ok(await _svc.GetUserReviewsAsync(userId, ct));
    }

    /// <summary>Чи може поточний користувач залишити відгук (є Used-квиток і сеанс завершився).</summary>
    [HttpGet("movies/{movieId:int}/can-review")]
    [Authorize]
    public async Task<IActionResult> CanReview(int movieId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var can    = await _svc.CanReviewAsync(userId, movieId, ct);
        var mine   = await _svc.GetUserReviewAsync(userId, movieId, ct);
        return Ok(new { canReview = can, myReview = mine });
    }

    /// <summary>Надіслати відгук.</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Submit([FromBody] SubmitReviewRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        try
        {
            var dto = await _svc.SubmitAsync(userId, request, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex)            { return BadRequest(ex.Message); }
    }

    /// <summary>Редагувати власний відгук.</summary>
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        try
        {
            return Ok(await _svc.UpdateAsync(userId, id, request, ct));
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex)            { return BadRequest(ex.Message); }
    }

    /// <summary>Видалити власний відгук. Admin може видалити будь-який відгук.</summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        try
        {
            await _svc.DeleteAsync(userId, id, isAdmin, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    // ── Admin ────────────────────────────────────────────────────────────────

    /// <summary>Усі відгуки для адмін-панелі.</summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminReviews(CancellationToken ct) =>
        Ok(await _svc.GetAllForAdminAsync(ct));
}

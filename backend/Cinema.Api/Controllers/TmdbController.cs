using Cinema.Application.Tmdb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/admin/tmdb")]
[Authorize(Roles = "Admin")]
public sealed class TmdbController(ITmdbService tmdb) : ControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query parameter 'q' is required.");

        var results = await tmdb.SearchMoviesAsync(q, ct);
        return Ok(results);
    }

    [HttpGet("{tmdbId:int}")]
    public async Task<IActionResult> GetDetail(int tmdbId, CancellationToken ct)
    {
        var detail = await tmdb.GetMovieDetailAsync(tmdbId, ct);
        if (detail is null)
            return NotFound();
        return Ok(detail);
    }

    [HttpGet("genres")]
    public async Task<IActionResult> Genres(CancellationToken ct)
        => Ok(await tmdb.GetGenresAsync(ct));

    [HttpGet("now-playing")]
    public async Task<IActionResult> NowPlaying(
        [FromQuery] int? genreId,
        [FromQuery] string? language,
        [FromQuery] string? sortBy,
        [FromQuery] int page,
        CancellationToken ct)
    {
        if (page < 1) page = 1;
        var filters = new TmdbDiscoverFilters(genreId, language, sortBy ?? "popularity.desc", page);
        var result  = await tmdb.GetNowPlayingAsync(filters, ct);
        return Ok(result);
    }
}

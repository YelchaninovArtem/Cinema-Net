using Cinema.Application.Genres;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/genres")]
public sealed class GenresController : ControllerBase
{
    private readonly IGenreQueryService _genres;

    public GenresController(IGenreQueryService genres) => _genres = genres;

    [NonAction]
    public async Task<IActionResult> GetGenres(CancellationToken ct) =>
        Ok(await _genres.GetAllAsync(ct));

    [HttpGet]
    public async Task<IActionResult> GetGenres([FromQuery] string? lang = null, CancellationToken ct = default) =>
        Ok(await _genres.GetAllAsync(lang, ct));

    [HttpPost("ensure")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EnsureGenres([FromBody] string[] names, CancellationToken ct) =>
        Ok(await _genres.EnsureAsync(names, ct));
}

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

    [HttpGet]
    public async Task<IActionResult> GetGenres(CancellationToken ct) =>
        Ok(await _genres.GetAllAsync(ct));

    [HttpPost("ensure")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EnsureGenres([FromBody] string[] names, CancellationToken ct) =>
        Ok(await _genres.EnsureAsync(names, ct));
}

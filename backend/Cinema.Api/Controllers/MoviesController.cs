using Cinema.Application.Movies;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MoviesController : ControllerBase
{
    private readonly IMovieQueryService _movies;

    public MoviesController(IMovieQueryService movies) => _movies = movies;

    [NonAction]
    public async Task<IActionResult> GetMovies(
        string? title,
        string? city,
        DateOnly? date,
        string? format,
        int? genreId,
        CancellationToken ct)
    {
        var filters = new MovieFilters(title, city, date, format, genreId);
        var result = await _movies.GetMoviesAsync(filters, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string? title,
        [FromQuery] string? city,
        [FromQuery] DateOnly? date,
        [FromQuery] string? format,
        [FromQuery] int? genreId,
        [FromQuery] string? lang,
        CancellationToken ct = default)
    {
        var filters = new MovieFilters(title, city, date, format, genreId);
        var result = await _movies.GetMoviesAsync(filters, lang, ct);
        return Ok(result);
    }

    [NonAction]
    public async Task<IActionResult> GetMovie(int id, CancellationToken ct)
    {
        var movie = await _movies.GetMovieByIdAsync(id, ct);
        return movie is null ? NotFound() : Ok(movie);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMovie(int id, [FromQuery] string? lang = null, CancellationToken ct = default)
    {
        var movie = await _movies.GetMovieByIdAsync(id, lang, ct);
        return movie is null ? NotFound() : Ok(movie);
    }
}

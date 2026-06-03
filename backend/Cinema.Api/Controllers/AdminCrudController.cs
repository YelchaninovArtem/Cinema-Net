using Cinema.Application.Cinemas;
using Cinema.Application.Halls;
using Cinema.Application.Movies;
using Cinema.Application.Showtimes;
using Cinema.Application.Users;
using Cinema.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminCrudController(
    ICinemaAdminService cinemas,
    IMovieAdminService movies,
    IHallAdminService halls,
    IShowtimeAdminService showtimes,
    IStaffUserService staffUsers) : ControllerBase
{
    // ═══════════════════ CINEMAS ═══════════════════

    [HttpGet("cinemas")]
    [ProducesResponseType(typeof(IReadOnlyList<CinemaAdminDto>), 200)]
    public async Task<IActionResult> GetAllCinemas(CancellationToken ct)
        => Ok(await cinemas.GetAllAsync(ct));

    [HttpGet("cinemas/{id:int}")]
    [ProducesResponseType(typeof(CinemaAdminDto), 200)]
    public async Task<IActionResult> GetCinema(int id, CancellationToken ct)
    {
        var cinema = await cinemas.GetByIdAsync(id, ct);
        return cinema is null ? NotFound() : Ok(cinema);
    }

    [HttpPost("cinemas")]
    [ProducesResponseType(201)]
    public async Task<IActionResult> CreateCinema(
        [FromBody] CreateCinemaRequest request,
        CancellationToken ct)
    {
        try
        {
            var id = await cinemas.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetCinema), new { id }, new { id });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("cinemas/{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> UpdateCinema(
        int id,
        [FromBody] UpdateCinemaRequest request,
        CancellationToken ct)
    {
        try
        {
            await cinemas.UpdateAsync(id, request, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { error = ex.Message })
                : BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("cinemas/{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteCinema(int id, CancellationToken ct)
    {
        try
        {
            await cinemas.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ═══════════════════ MOVIES ═══════════════════

    [HttpGet("movies")]
    [ProducesResponseType(typeof(IReadOnlyList<MovieAdminDto>), 200)]
    public async Task<IActionResult> GetAllMovies(CancellationToken ct)
        => Ok(await movies.GetAllAsync(ct));

    [HttpGet("movies/{id:int}")]
    [ProducesResponseType(typeof(MovieAdminDto), 200)]
    public async Task<IActionResult> GetMovie(int id, CancellationToken ct)
    {
        var movie = await movies.GetByIdAsync(id, ct);
        return movie is null ? NotFound() : Ok(movie);
    }

    [HttpPost("movies")]
    [ProducesResponseType(201)]
    public async Task<IActionResult> CreateMovie(
        [FromBody] CreateMovieRequest request,
        CancellationToken ct)
    {
        try
        {
            var id = await movies.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetMovie), new { id }, new { id });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("movies/{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> UpdateMovie(
        int id,
        [FromBody] UpdateMovieRequest request,
        CancellationToken ct)
    {
        try
        {
            await movies.UpdateAsync(id, request, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { error = ex.Message })
                : BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("movies/{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteMovie(int id, CancellationToken ct)
    {
        try
        {
            await movies.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ═══════════════════ HALLS ═══════════════════

    [HttpGet("halls")]
    [ProducesResponseType(typeof(IReadOnlyList<HallAdminDto>), 200)]
    public async Task<IActionResult> GetAllHalls(CancellationToken ct)
        => Ok(await halls.GetAllAsync(ct));

    [HttpGet("halls/cinema/{cinemaId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<HallAdminDto>), 200)]
    public async Task<IActionResult> GetHallsByCinema(int cinemaId, CancellationToken ct)
        => Ok(await halls.GetByCinemaAsync(cinemaId, ct));

    [HttpGet("halls/{id:int}")]
    [ProducesResponseType(typeof(HallAdminDto), 200)]
    public async Task<IActionResult> GetHall(int id, CancellationToken ct)
    {
        var hall = await halls.GetByIdAsync(id, ct);
        return hall is null ? NotFound() : Ok(hall);
    }

    [HttpPost("halls")]
    [ProducesResponseType(201)]
    public async Task<IActionResult> CreateHall(
        [FromBody] CreateHallRequest request,
        CancellationToken ct)
    {
        try
        {
            var id = await halls.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetHall), new { id }, new { id });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("halls/{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> UpdateHall(
        int id,
        [FromBody] UpdateHallRequest request,
        CancellationToken ct)
    {
        try
        {
            await halls.UpdateAsync(id, request, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { error = ex.Message })
                : BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("halls/{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteHall(int id, CancellationToken ct)
    {
        try
        {
            await halls.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ═══════════ SHOWTIMES ═══════════

    [HttpGet("showtimes")]
    [ProducesResponseType(typeof(IReadOnlyList<ShowtimeAdminDto>), 200)]
    public async Task<IActionResult> GetAllShowtimes(CancellationToken ct)
        => Ok(await showtimes.GetAllAsync(ct));

    [HttpGet("showtimes/cinema/{cinemaId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<ShowtimeAdminDto>), 200)]
    public async Task<IActionResult> GetShowtimesByCinema(int cinemaId, CancellationToken ct)
        => Ok(await showtimes.GetByCinemaAsync(cinemaId, ct));

    [HttpGet("showtimes/hall/{hallId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<ShowtimeAdminDto>), 200)]
    public async Task<IActionResult> GetShowtimesByHall(int hallId, CancellationToken ct)
        => Ok(await showtimes.GetByHallAsync(hallId, ct));

    [HttpGet("showtimes/{id:int}")]
    [ProducesResponseType(typeof(ShowtimeAdminDto), 200)]
    public async Task<IActionResult> GetShowtime(int id, CancellationToken ct)
    {
        var showtime = await showtimes.GetByIdAsync(id, ct);
        return showtime is null ? NotFound() : Ok(showtime);
    }

    [HttpPost("showtimes")]
    [ProducesResponseType(201)]
    public async Task<IActionResult> CreateShowtime(
        [FromBody] CreateShowtimeRequest request,
        CancellationToken ct)
    {
        try
        {
            var id = await showtimes.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetShowtime), new { id }, new { id });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("showtimes/{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> UpdateShowtime(
        int id,
        [FromBody] UpdateShowtimeRequest request,
        CancellationToken ct)
    {
        try
        {
            await showtimes.UpdateAsync(id, request, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { error = ex.Message })
                : BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("showtimes/{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteShowtime(int id, CancellationToken ct)
    {
        try
        {
            await showtimes.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("showtimes/check-conflict")]
    [ProducesResponseType(typeof(ShowtimeConflictResult), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CheckShowtimeConflict(
        [FromQuery] int hallId,
        [FromQuery] DateTime startUtc,
        [FromQuery] int? excludeShowtimeId,
        [FromQuery] DateTime endUtc,
        CancellationToken ct)
    {
        if (hallId <= 0)
            return BadRequest(new { error = "Invalid hallId." });
        if (endUtc <= startUtc)
            return BadRequest(new { error = "endUtc must be after startUtc." });

        var result = await showtimes.CheckConflictAsync(excludeShowtimeId, hallId, startUtc, endUtc, ct);
        return Ok(result);
    }

    // ═══════════════════ STAFF USERS ═══════════════════

    [HttpGet("staff")]
    [ProducesResponseType(typeof(IReadOnlyList<StaffUserDto>), 200)]
    public async Task<IActionResult> GetStaff(CancellationToken ct)
        => Ok(await staffUsers.GetStaffAsync(ct));

    [HttpPost("staff")]
    [ProducesResponseType(typeof(StaffUserDto), 201)]
    public async Task<IActionResult> CreateStaff(
        [FromBody] CreateStaffUserRequest request,
        [FromServices] IValidator<CreateStaffUserRequest> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors[0].ErrorMessage });

        try
        {
            var dto = await staffUsers.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetStaff), dto);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("staff/{id}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteStaff(string id, CancellationToken ct)
    {
        try
        {
            await staffUsers.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

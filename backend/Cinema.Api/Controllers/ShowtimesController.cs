using Cinema.Application.Showtimes;
using Cinema.Application.Tickets;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/showtimes")]
public sealed class ShowtimesController : ControllerBase
{
    private readonly IShowtimeQueryService _showtimes;
    private readonly ITicketService _ticketService;

    public ShowtimesController(IShowtimeQueryService showtimes, ITicketService ticketService)
    {
        _showtimes = showtimes;
        _ticketService   = ticketService;
    }

    [HttpGet]
    public async Task<IActionResult> GetShowtimes(
        [FromQuery] int? movieId,
        [FromQuery] string? city,
        [FromQuery] DateOnly? date,
        [FromQuery] string? format,
        CancellationToken ct)
    {
        var filters = new ShowtimeFilters(movieId, city, date, format);
        var result = await _showtimes.GetShowtimesAsync(filters, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}/seats")]
    public async Task<IActionResult> GetSeatMap(int id, CancellationToken ct)
    {
        var map = await _ticketService.GetSeatMapAsync(id, ct);
        return map is null ? NotFound() : Ok(map);
    }
}

using Cinema.Application.Cinemas;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/cinemas")]
public sealed class CinemasController : ControllerBase
{
    private readonly ICinemaQueryService _cinemas;

    public CinemasController(ICinemaQueryService cinemas) => _cinemas = cinemas;

    [HttpGet]
    public async Task<IActionResult> GetCinemas(CancellationToken ct) =>
        Ok(await _cinemas.GetAllAsync(ct));
}

namespace Cinema.Application.Showtimes;

public interface IShowtimeQueryService
{
    Task<IReadOnlyList<ShowtimeDto>> GetShowtimesAsync(ShowtimeFilters filters, CancellationToken ct = default);
}

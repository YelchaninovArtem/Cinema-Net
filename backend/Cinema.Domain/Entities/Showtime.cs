using Cinema.Domain.Common;
using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public sealed class Showtime
{
    private Showtime() { }

    public Showtime(int movieId, int hallId, DateTime startUtc, MovieFormat format, decimal basePrice)
    {
        MovieId = movieId;
        HallId = hallId;
        Reschedule(startUtc);
        Format = format;
        Reprice(basePrice);
    }

    public int Id { get; private set; }
    public int MovieId { get; private set; }
    public Movie Movie { get; private set; } = default!;
    public int HallId { get; private set; }
    public Hall Hall { get; private set; } = default!;
    public DateTime StartUtc { get; private set; }
    public MovieFormat Format { get; private set; }
    public decimal BasePrice { get; private set; }

    public void Reschedule(DateTime startUtc)
    {
        if (startUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("Showtime start must be UTC.");
        StartUtc = startUtc;
    }

    public void Reprice(decimal basePrice)
    {
        if (basePrice <= 0)
            throw new DomainException("Base price must be positive.");
        BasePrice = basePrice;
    }

    public void ChangeFormat(MovieFormat format)
    {
        Format = format;
    }
}

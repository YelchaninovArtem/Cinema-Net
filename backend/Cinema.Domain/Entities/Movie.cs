using Cinema.Domain.Common;
using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public sealed class Movie
{
    private readonly List<Genre> _genres = [];
    private readonly List<Showtime> _showtimes = [];

    private Movie() { }

    public Movie(
        string title,
        string description,
        int durationMinutes,
        AgeRating ageRating,
        DateTime releaseDateUtc,
        string? posterUrl = null,
        string? trailerUrl = null)
    {
        Rename(title);
        UpdateDescription(description);
        SetDuration(durationMinutes);
        AgeRating = ageRating;
        SetReleaseDate(releaseDateUtc);
        PosterUrl = posterUrl;
        TrailerUrl = trailerUrl;
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public int DurationMinutes { get; private set; }
    public AgeRating AgeRating { get; private set; }
    public DateTime ReleaseDateUtc { get; private set; }
    public string? PosterUrl { get; private set; }
    public string? TrailerUrl { get; private set; }

    public IReadOnlyCollection<Genre> Genres => _genres.AsReadOnly();
    public IReadOnlyCollection<Showtime> Showtimes => _showtimes.AsReadOnly();

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Movie title is required.");
        Title = title.Trim();
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Movie description is required.");
        Description = description.Trim();
    }

    public void SetDuration(int minutes)
    {
        if (minutes is < 1 or > 600)
            throw new DomainException("Movie duration must be between 1 and 600 minutes.");
        DurationMinutes = minutes;
    }

    public void SetReleaseDate(DateTime releaseDateUtc)
    {
        if (releaseDateUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("Release date must be UTC.");
        ReleaseDateUtc = releaseDateUtc;
    }

    public void AddGenre(Genre genre)
    {
        if (_genres.Any(g => g.Id == genre.Id))
            return;
        _genres.Add(genre);
    }

    public void SetPosterUrl(string? posterUrl)
    {
        PosterUrl = posterUrl;
    }

    public void SetTrailerUrl(string? trailerUrl)
    {
        TrailerUrl = trailerUrl;
    }
}

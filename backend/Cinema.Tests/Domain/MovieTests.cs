using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using FluentAssertions;

namespace Cinema.Tests.Domain;

public sealed class MovieTests
{
    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Creates_movie_with_valid_data()
    {
        var movie = new Movie("Neon Pulse", "Courier chase", 109, AgeRating.R, Utc(2026, 3, 20));

        movie.Title.Should().Be("Neon Pulse");
        movie.DurationMinutes.Should().Be(109);
        movie.AgeRating.Should().Be(AgeRating.R);
    }

    [Fact]
    public void Rejects_non_utc_release_date()
    {
        var localDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Local);
        var act = () => new Movie("X", "Y", 100, AgeRating.PG, localDate);
        act.Should().Throw<DomainException>().WithMessage("*UTC*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(601)]
    public void Rejects_invalid_duration(int minutes)
    {
        var act = () => new Movie("X", "Y", minutes, AgeRating.PG, Utc(2026, 1, 1));
        act.Should().Throw<DomainException>().WithMessage("*duration*");
    }

    [Fact]
    public void Add_genre_is_idempotent_when_genre_has_same_id()
    {
        var movie = new Movie("X", "Y", 100, AgeRating.PG, Utc(2026, 1, 1));
        var genre = new Genre("Drama");
        SetId(genre, 42);

        movie.AddGenre(genre);
        movie.AddGenre(genre);

        movie.Genres.Should().ContainSingle().Which.Should().BeSameAs(genre);
    }

    // Утиліта лише для тестів: EF зазвичай проставляє Id через материалізацію, у юніт-тестах імітуємо це рефлексією.
    private static void SetId(Genre genre, int id) =>
        typeof(Genre).GetProperty(nameof(Genre.Id))!.SetValue(genre, id);
}

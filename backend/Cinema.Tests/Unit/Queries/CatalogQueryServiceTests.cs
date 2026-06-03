using Cinema.Application.Movies;
using Cinema.Application.Showtimes;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using Cinema.Infrastructure.Queries;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Tests.Unit.Queries;

public sealed class CatalogQueryServiceTests : IDisposable
{
    private readonly CinemaDbContext _db;

    public CatalogQueryServiceTests()
    {
        var opts = new DbContextOptionsBuilder<CinemaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CinemaDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GenreGetAllAsync_ReturnsGenresOrderedByName()
    {
        _db.Genres.AddRange(new Genre("Drama"), new Genre("Action"), new Genre("Comedy"));
        await _db.SaveChangesAsync();
        var service = new GenreQueryService(_db);

        var genres = await service.GetAllAsync();

        genres.Select(g => g.Name).Should().Equal("Action", "Comedy", "Drama");
    }

    [Fact]
    public async Task GenreEnsureAsync_TrimsDeduplicatesAndCreatesMissingGenres()
    {
        _db.Genres.Add(new Genre("Action"));
        await _db.SaveChangesAsync();
        var service = new GenreQueryService(_db);

        var genres = await service.EnsureAsync([" Action ", "Drama", "", "drama", "  "]);

        genres.Select(g => g.Name).Should().Equal("Action", "Drama");
        (await _db.Genres.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task GenreEnsureAsync_ReturnsEmpty_WhenOnlyBlankNamesProvided()
    {
        var service = new GenreQueryService(_db);

        var genres = await service.EnsureAsync(["", "  "]);

        genres.Should().BeEmpty();
        (await _db.Genres.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CinemaGetAllAsync_ReturnsCinemasOrderedByCityThenName()
    {
        _db.CinemaBranches.AddRange(
            new CinemaBranch("Zeta", "Lviv", "Address 1", "Europe/Kyiv"),
            new CinemaBranch("Beta", "Kyiv", "Address 2", "Europe/Kyiv"),
            new CinemaBranch("Alpha", "Kyiv", "Address 3", "Europe/Kyiv"));
        await _db.SaveChangesAsync();
        var service = new CinemaQueryService(_db);

        var cinemas = await service.GetAllAsync();

        cinemas.Select(c => c.Name).Should().Equal("Alpha", "Beta", "Zeta");
    }

    [Fact]
    public async Task MovieGetMoviesAsync_ReturnsOnlyMoviesWithFutureShowtimesAndAvailableFormats()
    {
        var genre = new Genre("Sci-Fi");
        _db.Genres.Add(genre);
        await _db.SaveChangesAsync();

        var futureMovie = new Movie("Arrival", "Future showtime", 116, AgeRating.PG13, Utc(2026, 1, 1));
        futureMovie.AddGenre(genre);
        var pastMovie = new Movie("Old News", "Past showtime", 90, AgeRating.PG, Utc(2025, 1, 1));
        pastMovie.AddGenre(genre);
        _db.Movies.AddRange(futureMovie, pastMovie);
        await _db.SaveChangesAsync();

        var (kyivHall, _) = await AddBranchesAndHallsAsync();
        _db.Showtimes.AddRange(
            new Showtime(futureMovie.Id, kyivHall.Id, DateTime.UtcNow.AddDays(2), MovieFormat.Imax, 250m),
            new Showtime(pastMovie.Id, kyivHall.Id, DateTime.UtcNow.AddDays(-2), MovieFormat.TwoD, 120m));
        await _db.SaveChangesAsync();

        var service = new MovieQueryService(_db);

        var movies = await service.GetMoviesAsync(new MovieFilters());

        movies.Should().ContainSingle();
        movies[0].Title.Should().Be("Arrival");
        movies[0].AvailableFormats.Should().Equal("IMAX");
        movies[0].Genres.Should().Equal("Sci-Fi");
    }

    [Fact]
    public async Task MovieGetMoviesAsync_AppliesCityDateFormatGenreAndTitleFilters()
    {
        var action = new Genre("Action");
        var drama = new Genre("Drama");
        _db.Genres.AddRange(action, drama);
        await _db.SaveChangesAsync();

        var target = new Movie("Kyiv Target", "Target movie", 100, AgeRating.PG13, Utc(2026, 1, 1));
        target.AddGenre(action);
        var otherGenre = new Movie("Kyiv Drama", "Wrong genre", 100, AgeRating.PG13, Utc(2026, 1, 1));
        otherGenre.AddGenre(drama);
        var otherCity = new Movie("Lviv Target", "Wrong city", 100, AgeRating.PG13, Utc(2026, 1, 1));
        otherCity.AddGenre(action);
        _db.Movies.AddRange(target, otherGenre, otherCity);
        await _db.SaveChangesAsync();

        var (kyivHall, lvivHall) = await AddBranchesAndHallsAsync();
        var date = DateTime.UtcNow.Date.AddDays(3);
        _db.Showtimes.AddRange(
            new Showtime(target.Id, kyivHall.Id, date.AddHours(12), MovieFormat.ThreeD, 180m),
            new Showtime(otherGenre.Id, kyivHall.Id, date.AddHours(13), MovieFormat.ThreeD, 180m),
            new Showtime(otherCity.Id, lvivHall.Id, date.AddHours(12), MovieFormat.ThreeD, 180m));
        await _db.SaveChangesAsync();

        var service = new MovieQueryService(_db);

        var movies = await service.GetMoviesAsync(new MovieFilters(
            City: "Kyiv",
            Date: DateOnly.FromDateTime(date),
            Format: "3D",
            GenreId: action.Id,
            Title: " Target "));

        movies.Should().ContainSingle();
        movies[0].Id.Should().Be(target.Id);
        movies[0].AvailableFormats.Should().Equal("3D");
    }

    [Fact]
    public async Task MovieGetMoviesAsync_IgnoresUnknownFormatFilter()
    {
        var genre = new Genre("Adventure");
        _db.Genres.Add(genre);
        await _db.SaveChangesAsync();

        var movie = new Movie("Open Format", "Unknown format should not filter out", 100, AgeRating.PG13, Utc(2026, 1, 1));
        movie.AddGenre(genre);
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

        var (kyivHall, _) = await AddBranchesAndHallsAsync();
        _db.Showtimes.Add(new Showtime(movie.Id, kyivHall.Id, DateTime.UtcNow.AddDays(1), MovieFormat.TwoD, 150m));
        await _db.SaveChangesAsync();

        var service = new MovieQueryService(_db);

        var movies = await service.GetMoviesAsync(new MovieFilters(Format: "ScreenX"));

        movies.Should().ContainSingle();
        movies[0].AvailableFormats.Should().Equal("2D");
    }

    [Fact]
    public async Task MovieGetMovieByIdAsync_ReturnsMovieDetailWithSortedGenres()
    {
        var action = new Genre("Action");
        var drama = new Genre("Drama");
        _db.Genres.AddRange(action, drama);
        await _db.SaveChangesAsync();

        var movie = new Movie(
            "Details",
            "Detailed description",
            120,
            AgeRating.R,
            Utc(2026, 1, 1),
            "poster.jpg",
            "trailer.mp4");
        movie.AddGenre(drama);
        movie.AddGenre(action);
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

        var service = new MovieQueryService(_db);

        var detail = await service.GetMovieByIdAsync(movie.Id);

        detail.Should().NotBeNull();
        detail!.Title.Should().Be("Details");
        detail.Description.Should().Be("Detailed description");
        detail.PosterUrl.Should().Be("poster.jpg");
        detail.TrailerUrl.Should().Be("trailer.mp4");
        detail.AgeRating.Should().Be("R");
        detail.Genres.Should().Equal("Action", "Drama");
    }

    [Fact]
    public async Task MovieGetMovieByIdAsync_ReturnsNull_WhenMovieMissing()
    {
        var service = new MovieQueryService(_db);

        var movie = await service.GetMovieByIdAsync(404);

        movie.Should().BeNull();
    }

    [Fact]
    public async Task ShowtimeGetShowtimesAsync_AppliesFiltersAndMarksStartAsUtc()
    {
        var movie = new Movie("Filtered", "Filtered showtime", 100, AgeRating.PG13, Utc(2026, 1, 1));
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

        var (kyivHall, lvivHall) = await AddBranchesAndHallsAsync();
        var date = DateTime.UtcNow.Date.AddDays(4);
        _db.Showtimes.AddRange(
            new Showtime(movie.Id, kyivHall.Id, date.AddHours(10), MovieFormat.TwoD, 150m),
            new Showtime(movie.Id, lvivHall.Id, date.AddHours(10), MovieFormat.Imax, 220m),
            new Showtime(movie.Id, kyivHall.Id, DateTime.UtcNow.AddDays(-1), MovieFormat.TwoD, 150m));
        await _db.SaveChangesAsync();

        var service = new ShowtimeQueryService(_db);

        var showtimes = await service.GetShowtimesAsync(new ShowtimeFilters(
            MovieId: movie.Id,
            City: "Kyiv",
            Date: DateOnly.FromDateTime(date),
            Format: "2D"));

        showtimes.Should().ContainSingle();
        showtimes[0].City.Should().Be("Kyiv");
        showtimes[0].Format.Should().Be("2D");
        showtimes[0].StartUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task ShowtimeGetShowtimesAsync_IgnoresUnknownFormatFilter()
    {
        var movie = new Movie("Any Format", "Unknown filter is ignored", 100, AgeRating.PG13, Utc(2026, 1, 1));
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

        var (kyivHall, _) = await AddBranchesAndHallsAsync();
        _db.Showtimes.Add(new Showtime(movie.Id, kyivHall.Id, DateTime.UtcNow.AddDays(1), MovieFormat.Imax, 220m));
        await _db.SaveChangesAsync();

        var service = new ShowtimeQueryService(_db);

        var showtimes = await service.GetShowtimesAsync(new ShowtimeFilters(Format: "ScreenX"));

        showtimes.Should().ContainSingle();
        showtimes[0].Format.Should().Be("IMAX");
    }

    private async Task<(Hall KyivHall, Hall LvivHall)> AddBranchesAndHallsAsync()
    {
        var kyiv = new CinemaBranch("Central", "Kyiv", "Khreshchatyk", "Europe/Kyiv");
        var lviv = new CinemaBranch("Forum", "Lviv", "Pid Dubom", "Europe/Kyiv");
        _db.CinemaBranches.AddRange(kyiv, lviv);
        await _db.SaveChangesAsync();

        var layout = new[] { new[] { SeatTypeCode.Standard, SeatTypeCode.Standard } };
        var kyivHall = new Hall(kyiv.Id, "Blue", 1, 2, layout);
        var lvivHall = new Hall(lviv.Id, "Green", 1, 2, layout);
        _db.Halls.AddRange(kyivHall, lvivHall);
        await _db.SaveChangesAsync();

        return (kyivHall, lvivHall);
    }

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}

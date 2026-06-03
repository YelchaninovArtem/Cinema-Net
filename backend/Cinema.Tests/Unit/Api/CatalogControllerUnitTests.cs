using Cinema.Api.Controllers;
using Cinema.Application.Cinemas;
using Cinema.Application.Genres;
using Cinema.Application.Movies;
using Cinema.Application.Showtimes;
using Cinema.Application.Tickets;
using Cinema.Application.Tmdb;
using Cinema.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cinema.Tests.Unit.Api;

public sealed class CatalogControllerUnitTests
{
    [Fact]
    public async Task MoviesGetMovies_PassesFiltersToServiceAndReturnsOk()
    {
        var service = new Mock<IMovieQueryService>();
        IReadOnlyList<MovieSummaryDto> expected =
        [
            new(1, "Movie", null, 100, "PG13", ["Drama"], ["2D"])
        ];
        service.Setup(s => s.GetMoviesAsync(
                It.Is<MovieFilters>(f =>
                    f.Title == "mov" &&
                    f.City == "Kyiv" &&
                    f.Date == new DateOnly(2026, 6, 3) &&
                    f.Format == "2D" &&
                    f.GenreId == 7),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new MoviesController(service.Object);

        var result = await controller.GetMovies("mov", "Kyiv", new DateOnly(2026, 6, 3), "2D", 7, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task MoviesGetMovie_ReturnsNotFound_WhenMovieMissing()
    {
        var service = new Mock<IMovieQueryService>();
        service.Setup(s => s.GetMovieByIdAsync(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MovieDetailDto?)null);
        var controller = new MoviesController(service.Object);

        var result = await controller.GetMovie(404, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MoviesGetMovie_ReturnsOk_WhenMovieExists()
    {
        var detail = new MovieDetailDto(1, "Movie", "Description", null, null, 100, "PG13", DateTime.UtcNow, ["Drama"]);
        var service = new Mock<IMovieQueryService>();
        service.Setup(s => s.GetMovieByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);
        var controller = new MoviesController(service.Object);

        var result = await controller.GetMovie(1, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(detail);
    }

    [Fact]
    public async Task ShowtimesGetShowtimes_PassesFiltersToServiceAndReturnsOk()
    {
        var service = new Mock<IShowtimeQueryService>();
        var tickets = new Mock<ITicketService>();
        IReadOnlyList<ShowtimeDto> expected =
        [
            new(1, 2, "Movie", "Hall", "Branch", "Kyiv", DateTime.UtcNow, "IMAX", 200m)
        ];
        service.Setup(s => s.GetShowtimesAsync(
                It.Is<ShowtimeFilters>(f =>
                    f.MovieId == 2 &&
                    f.City == "Kyiv" &&
                    f.Date == new DateOnly(2026, 6, 3) &&
                    f.Format == "IMAX"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new ShowtimesController(service.Object, tickets.Object);

        var result = await controller.GetShowtimes(2, "Kyiv", new DateOnly(2026, 6, 3), "IMAX", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task ShowtimesGetSeatMap_ReturnsNotFound_WhenMapMissing()
    {
        var service = new Mock<IShowtimeQueryService>();
        var tickets = new Mock<ITicketService>();
        tickets.Setup(s => s.GetSeatMapAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeatMapDto?)null);
        var controller = new ShowtimesController(service.Object, tickets.Object);

        var result = await controller.GetSeatMap(10, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ShowtimesGetSeatMap_ReturnsOk_WhenMapExists()
    {
        var map = new SeatMapDto(
            10,
            "Movie",
            "Hall",
            "Branch",
            "Kyiv",
            DateTime.UtcNow,
            "2D",
            150m,
            1,
            1,
            [[SeatTypeCode.Standard]],
            []);
        var service = new Mock<IShowtimeQueryService>();
        var tickets = new Mock<ITicketService>();
        tickets.Setup(s => s.GetSeatMapAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(map);
        var controller = new ShowtimesController(service.Object, tickets.Object);

        var result = await controller.GetSeatMap(10, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(map);
    }

    [Fact]
    public async Task GenresGetGenres_ReturnsOk()
    {
        var service = new Mock<IGenreQueryService>();
        IReadOnlyList<GenreDto> genres = [new(1, "Drama")];
        service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(genres);
        var controller = new GenresController(service.Object);

        var result = await controller.GetGenres(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(genres);
    }

    [Fact]
    public async Task GenresEnsureGenres_ReturnsOk()
    {
        var service = new Mock<IGenreQueryService>();
        IReadOnlyList<GenreDto> genres = [new(1, "Drama")];
        service.Setup(s => s.EnsureAsync(It.Is<string[]>(names => names.SequenceEqual(new[] { "Drama" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(genres);
        var controller = new GenresController(service.Object);

        var result = await controller.EnsureGenres(["Drama"], CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(genres);
    }

    [Fact]
    public async Task CinemasGetCinemas_ReturnsOk()
    {
        var service = new Mock<ICinemaQueryService>();
        IReadOnlyList<CinemaBranchDto> cinemas = [new(1, "Central", "Kyiv", "Street")];
        service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cinemas);
        var controller = new CinemasController(service.Object);

        var result = await controller.GetCinemas(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(cinemas);
    }

    [Fact]
    public async Task TmdbSearch_ReturnsBadRequest_WhenQueryBlank()
    {
        var service = new Mock<ITmdbService>();
        var controller = new TmdbController(service.Object);

        var result = await controller.Search(" ", CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be("Query parameter 'q' is required.");
    }

    [Fact]
    public async Task TmdbSearch_ReturnsOk_WhenQueryProvided()
    {
        var service = new Mock<ITmdbService>();
        IReadOnlyList<TmdbMovieSearchResult> movies = [new(1, "Movie", 2026, null, ["Drama"])];
        service.Setup(s => s.SearchMoviesAsync("Movie", It.IsAny<CancellationToken>())).ReturnsAsync(movies);
        var controller = new TmdbController(service.Object);

        var result = await controller.Search("Movie", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(movies);
    }

    [Fact]
    public async Task TmdbGetDetail_ReturnsNotFound_WhenMissing()
    {
        var service = new Mock<ITmdbService>();
        service.Setup(s => s.GetMovieDetailAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TmdbMovieDetail?)null);
        var controller = new TmdbController(service.Object);

        var result = await controller.GetDetail(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task TmdbGetDetail_ReturnsOk_WhenFound()
    {
        var detail = new TmdbMovieDetail(1, "Movie", "Description", 100, "PG13", DateTime.UtcNow, null, null, ["Drama"]);
        var service = new Mock<ITmdbService>();
        service.Setup(s => s.GetMovieDetailAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(detail);
        var controller = new TmdbController(service.Object);

        var result = await controller.GetDetail(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(detail);
    }

    [Fact]
    public async Task TmdbGenres_ReturnsOk()
    {
        var service = new Mock<ITmdbService>();
        IReadOnlyList<TmdbGenre> genres = [new(1, "Drama")];
        service.Setup(s => s.GetGenresAsync(It.IsAny<CancellationToken>())).ReturnsAsync(genres);
        var controller = new TmdbController(service.Object);

        var result = await controller.Genres(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(genres);
    }

    [Fact]
    public async Task TmdbNowPlaying_NormalizesInvalidPageAndDefaultSort()
    {
        var service = new Mock<ITmdbService>();
        var page = new TmdbMoviePageResult([], 1, 1);
        service.Setup(s => s.GetNowPlayingAsync(
                It.Is<TmdbDiscoverFilters>(f =>
                    f.GenreId == 5 &&
                    f.OriginalLanguage == "en" &&
                    f.SortBy == "popularity.desc" &&
                    f.Page == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);
        var controller = new TmdbController(service.Object);

        var result = await controller.NowPlaying(5, "en", null, 0, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(page);
    }
}

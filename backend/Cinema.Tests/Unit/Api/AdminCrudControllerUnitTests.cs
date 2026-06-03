using Cinema.Api.Controllers;
using Cinema.Application.Cinemas;
using Cinema.Application.Halls;
using Cinema.Application.Movies;
using Cinema.Application.Showtimes;
using Cinema.Application.Users;
using Cinema.Domain.Common;
using Cinema.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cinema.Tests.Unit.Api;

public sealed class AdminCrudControllerUnitTests
{
    [Fact]
    public async Task CinemaCrud_MapsCommonResults()
    {
        var cinemas = new Mock<ICinemaAdminService>();
        IReadOnlyList<CinemaAdminDto> all = [CinemaDto()];
        cinemas.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(all);
        cinemas.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CinemaDto());
        cinemas.Setup(s => s.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((CinemaAdminDto?)null);
        cinemas.Setup(s => s.CreateAsync(It.IsAny<CreateCinemaRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(5);
        cinemas.Setup(s => s.UpdateAsync(404, It.IsAny<UpdateCinemaRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Cinema not found."));
        cinemas.Setup(s => s.DeleteAsync(404, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Cinema not found."));
        var controller = Create(cinemas: cinemas.Object);

        (await controller.GetAllCinemas(CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetCinema(1, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetCinema(404, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.CreateCinema(new CreateCinemaRequest("Name", "City", "Address"), CancellationToken.None))
            .Should().BeOfType<CreatedAtActionResult>();
        (await controller.UpdateCinema(1, new UpdateCinemaRequest("Name", "City", "Address"), CancellationToken.None))
            .Should().BeOfType<NoContentResult>();
        (await controller.UpdateCinema(404, new UpdateCinemaRequest("Name", "City", "Address"), CancellationToken.None))
            .Should().BeOfType<NotFoundObjectResult>();
        (await controller.DeleteCinema(1, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.DeleteCinema(404, CancellationToken.None)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MovieCrud_MapsCommonResults()
    {
        var movies = new Mock<IMovieAdminService>();
        IReadOnlyList<MovieAdminDto> all = [MovieDto()];
        movies.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(all);
        movies.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MovieDto());
        movies.Setup(s => s.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((MovieAdminDto?)null);
        movies.Setup(s => s.CreateAsync(It.IsAny<CreateMovieRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(5);
        movies.Setup(s => s.CreateAsync(It.Is<CreateMovieRequest>(r => r.Title == ""), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Title required."));
        movies.Setup(s => s.UpdateAsync(404, It.IsAny<UpdateMovieRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Movie not found."));
        movies.Setup(s => s.DeleteAsync(404, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Movie not found."));
        var controller = Create(movies: movies.Object);

        (await controller.GetAllMovies(CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetMovie(1, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetMovie(404, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.CreateMovie(CreateMovie("Movie"), CancellationToken.None)).Should().BeOfType<CreatedAtActionResult>();
        (await controller.CreateMovie(CreateMovie(""), CancellationToken.None)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.UpdateMovie(1, UpdateMovie("Movie"), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.UpdateMovie(404, UpdateMovie("Movie"), CancellationToken.None)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.DeleteMovie(1, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.DeleteMovie(404, CancellationToken.None)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task HallCrud_MapsCommonResults()
    {
        var halls = new Mock<IHallAdminService>();
        IReadOnlyList<HallAdminDto> all = [HallDto()];
        halls.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(all);
        halls.Setup(s => s.GetByCinemaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(all);
        halls.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(HallDto());
        halls.Setup(s => s.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((HallAdminDto?)null);
        halls.Setup(s => s.CreateAsync(It.IsAny<CreateHallRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(5);
        halls.Setup(s => s.UpdateAsync(404, It.IsAny<UpdateHallRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Hall not found."));
        halls.Setup(s => s.DeleteAsync(404, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Hall not found."));
        var controller = Create(halls: halls.Object);

        (await controller.GetAllHalls(CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetHallsByCinema(1, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetHall(1, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetHall(404, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.CreateHall(CreateHall(), CancellationToken.None)).Should().BeOfType<CreatedAtActionResult>();
        (await controller.UpdateHall(1, UpdateHall(), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.UpdateHall(404, UpdateHall(), CancellationToken.None)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.DeleteHall(1, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.DeleteHall(404, CancellationToken.None)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ShowtimeCrudAndConflict_MapsCommonResults()
    {
        var showtimes = new Mock<IShowtimeAdminService>();
        IReadOnlyList<ShowtimeAdminDto> all = [ShowtimeDto()];
        showtimes.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(all);
        showtimes.Setup(s => s.GetByCinemaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(all);
        showtimes.Setup(s => s.GetByHallAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(all);
        showtimes.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ShowtimeDto());
        showtimes.Setup(s => s.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((ShowtimeAdminDto?)null);
        showtimes.Setup(s => s.CreateAsync(It.IsAny<CreateShowtimeRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(5);
        showtimes.Setup(s => s.UpdateAsync(404, It.IsAny<UpdateShowtimeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Showtime not found."));
        showtimes.Setup(s => s.DeleteAsync(400, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Cannot delete."));
        var conflict = new ShowtimeConflictResult(true, 1, "Movie", Utc(10), Utc(12));
        showtimes.Setup(s => s.CheckConflictAsync(null, 2, Utc(10), Utc(12), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflict);
        var controller = Create(showtimes: showtimes.Object);

        (await controller.GetAllShowtimes(CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetShowtimesByCinema(1, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetShowtimesByHall(2, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetShowtime(1, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.GetShowtime(404, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.CreateShowtime(CreateShowtime(), CancellationToken.None)).Should().BeOfType<CreatedAtActionResult>();
        (await controller.UpdateShowtime(1, UpdateShowtime(), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.UpdateShowtime(404, UpdateShowtime(), CancellationToken.None)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.DeleteShowtime(1, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.DeleteShowtime(400, CancellationToken.None)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.CheckShowtimeConflict(0, Utc(10), null, Utc(12), CancellationToken.None)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.CheckShowtimeConflict(2, Utc(12), null, Utc(10), CancellationToken.None)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.CheckShowtimeConflict(2, Utc(10), null, Utc(12), CancellationToken.None)).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task StaffCrud_ValidatesAndMapsResults()
    {
        var staff = new Mock<IStaffUserService>();
        IReadOnlyList<StaffUserDto> all = [new("id", "admin@example.com", "Ada", "Lovelace", "Admin")];
        var request = new CreateStaffUserRequest("admin@example.com", "secret1", "Ada", "Lovelace", "Admin");
        staff.Setup(s => s.GetStaffAsync(It.IsAny<CancellationToken>())).ReturnsAsync(all);
        staff.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(all[0]);
        staff.Setup(s => s.DeleteAsync("missing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("User not found."));
        var validValidator = Validator(true);
        var invalidValidator = Validator(false);
        var controller = Create(staffUsers: staff.Object);

        (await controller.GetStaff(CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.CreateStaff(request, validValidator.Object, CancellationToken.None)).Should().BeOfType<CreatedAtActionResult>();
        (await controller.CreateStaff(request, invalidValidator.Object, CancellationToken.None)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.DeleteStaff("id", CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.DeleteStaff("missing", CancellationToken.None)).Should().BeOfType<NotFoundObjectResult>();
    }

    private static AdminCrudController Create(
        ICinemaAdminService? cinemas = null,
        IMovieAdminService? movies = null,
        IHallAdminService? halls = null,
        IShowtimeAdminService? showtimes = null,
        IStaffUserService? staffUsers = null) =>
        new(
            cinemas ?? Mock.Of<ICinemaAdminService>(),
            movies ?? Mock.Of<IMovieAdminService>(),
            halls ?? Mock.Of<IHallAdminService>(),
            showtimes ?? Mock.Of<IShowtimeAdminService>(),
            staffUsers ?? Mock.Of<IStaffUserService>());

    private static Mock<IValidator<CreateStaffUserRequest>> Validator(bool valid)
    {
        var validator = new Mock<IValidator<CreateStaffUserRequest>>();
        var result = valid
            ? new ValidationResult()
            : new ValidationResult([new ValidationFailure("Email", "Invalid email.")]);
        validator.Setup(v => v.ValidateAsync(It.IsAny<CreateStaffUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return validator;
    }

    private static CinemaAdminDto CinemaDto() => new(1, "Cinema", "Kyiv", "Address", "Europe/Kyiv");
    private static MovieAdminDto MovieDto() => new(1, "Movie", "Description", 100, AgeRating.PG13, Utc(), null, null, [1]);
    private static HallAdminDto HallDto() => new(1, 1, "Cinema", "Hall", 1, 1, [[SeatTypeCode.Standard]]);
    private static ShowtimeAdminDto ShowtimeDto() => new(1, 1, "Movie", 2, "Hall", 3, "Cinema", Utc(10), MovieFormat.TwoD, 120m);
    private static CreateMovieRequest CreateMovie(string title) => new(title, "Description", 100, AgeRating.PG13, Utc(), null, null, [1]);
    private static UpdateMovieRequest UpdateMovie(string title) => new(title, "Description", 100, AgeRating.PG13, Utc(), null, null, [1]);
    private static CreateHallRequest CreateHall() => new(1, "Hall", 1, 1, [[SeatTypeCode.Standard]]);
    private static UpdateHallRequest UpdateHall() => new("Hall", 1, 1, [[SeatTypeCode.Standard]]);
    private static CreateShowtimeRequest CreateShowtime() => new(1, 2, Utc(10), MovieFormat.TwoD, 120m);
    private static UpdateShowtimeRequest UpdateShowtime() => new(Utc(10), MovieFormat.TwoD, 120m);
    private static DateTime Utc(int hour = 0) => new(2026, 1, 1, hour, 0, 0, DateTimeKind.Utc);
}

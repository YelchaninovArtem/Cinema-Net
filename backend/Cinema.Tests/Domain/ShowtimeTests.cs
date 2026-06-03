using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using FluentAssertions;

namespace Cinema.Tests.Domain;

public sealed class ShowtimeTests
{
    private static DateTime Utc(int year, int month, int day, int hour) => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Creates_showtime_with_valid_data()
    {
        var showtime = new Showtime(movieId: 1, hallId: 2, Utc(2026, 4, 15, 14), MovieFormat.Imax, 220m);

        showtime.MovieId.Should().Be(1);
        showtime.HallId.Should().Be(2);
        showtime.Format.Should().Be(MovieFormat.Imax);
        showtime.BasePrice.Should().Be(220m);
    }

    [Fact]
    public void Rejects_non_utc_start()
    {
        var local = new DateTime(2026, 4, 15, 14, 0, 0, DateTimeKind.Local);
        var act = () => new Showtime(1, 1, local, MovieFormat.TwoD, 150m);
        act.Should().Throw<DomainException>().WithMessage("*UTC*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Rejects_non_positive_price(decimal price)
    {
        var act = () => new Showtime(1, 1, Utc(2026, 4, 15, 14), MovieFormat.TwoD, price);
        act.Should().Throw<DomainException>().WithMessage("*positive*");
    }
}

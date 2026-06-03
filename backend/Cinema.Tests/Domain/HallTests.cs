using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using FluentAssertions;

namespace Cinema.Tests.Domain;

public sealed class HallTests
{
    [Fact]
    public void Round_trips_layout_through_json()
    {
        var layout = new[]
        {
            new[] { SeatTypeCode.Standard, SeatTypeCode.Standard, SeatTypeCode.Vip },
            new[] { SeatTypeCode.Standard, SeatTypeCode.Vip, SeatTypeCode.Love }
        };

        var hall = new Hall(cinemaBranchId: 1, name: "H1", rows: 2, cols: 3, layout);

        hall.GetLayout().Should().BeEquivalentTo(layout);
        hall.SeatTypeAt(1, 1).Should().Be(SeatTypeCode.Standard);
        hall.SeatTypeAt(2, 3).Should().Be(SeatTypeCode.Love);
    }

    [Fact]
    public void Rejects_mismatched_layout_dimensions()
    {
        var layout = new[]
        {
            new[] { SeatTypeCode.Standard, SeatTypeCode.Standard }
        };

        var act = () => new Hall(1, "H1", rows: 2, cols: 2, layout);
        act.Should().Throw<DomainException>().WithMessage("*rows count*");
    }

    [Fact]
    public void Rejects_out_of_bounds_access()
    {
        var hall = new Hall(1, "H1", 2, 2,
            [[SeatTypeCode.Standard, SeatTypeCode.Standard],
             [SeatTypeCode.Standard, SeatTypeCode.Standard]]);

        var act = () => hall.SeatTypeAt(3, 1);
        act.Should().Throw<DomainException>().WithMessage("*outside hall bounds*");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(41, 1)]
    [InlineData(1, 41)]
    public void Rejects_rows_or_cols_outside_allowed_range(int rows, int cols)
    {
        var layout = new SeatTypeCode[Math.Max(rows, 1)][];
        for (var r = 0; r < layout.Length; r++)
            layout[r] = new SeatTypeCode[Math.Max(cols, 1)];

        var act = () => new Hall(1, "H1", rows, cols, layout);
        act.Should().Throw<DomainException>();
    }
}

using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using FluentAssertions;

namespace Cinema.Tests.Domain;

public sealed class CinemaBranchTests
{
    [Fact]
    public void Creates_branch_with_trimmed_values()
    {
        var branch = new CinemaBranch("  Nova  ", "  Kyiv ", " Podil 10 ", "Europe/Kyiv");

        branch.Name.Should().Be("Nova");
        branch.City.Should().Be("Kyiv");
        branch.Address.Should().Be("Podil 10");
        branch.TimezoneId.Should().Be("Europe/Kyiv");
    }

    [Theory]
    [InlineData("", "Kyiv", "Addr", "Europe/Kyiv")]
    [InlineData("Nova", " ", "Addr", "Europe/Kyiv")]
    [InlineData("Nova", "Kyiv", "", "Europe/Kyiv")]
    [InlineData("Nova", "Kyiv", "Addr", " ")]
    public void Rejects_blank_required_fields(string name, string city, string address, string tz)
    {
        var act = () => new CinemaBranch(name, city, address, tz);
        act.Should().Throw<DomainException>();
    }
}

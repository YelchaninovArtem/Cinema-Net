using System.Security.Claims;
using Cinema.Api;
using FluentAssertions;

namespace Cinema.Tests.Unit.Api;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ReturnsSubjectClaim()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "user-123")
        ]));

        user.GetUserId().Should().Be("user-123");
    }

    [Fact]
    public void GetUserId_Throws_WhenSubjectClaimMissing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var act = () => user.GetUserId();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("User ID claim not found.");
    }
}

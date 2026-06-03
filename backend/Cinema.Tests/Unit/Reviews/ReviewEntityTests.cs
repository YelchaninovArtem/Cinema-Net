using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using FluentAssertions;

namespace Cinema.Tests.Unit.Reviews;

/// <summary>Unit-тести для доменної сутності Review.</summary>
public sealed class ReviewEntityTests
{
    [Fact]
    public void Review_ValidArgs_CreatesVisibleReview()
    {
        var r = new Review("u1", 1, 8, "Great movie!");
        r.IsApproved.Should().BeTrue();
        r.Rating.Should().Be(8);
        r.Comment.Should().Be("Great movie!");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void Review_InvalidRating_ThrowsDomainException(int rating)
    {
        var act = () => new Review("u1", 1, rating, "text");
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Review_EmptyComment_ThrowsDomainException(string comment)
    {
        var act = () => new Review("u1", 1, 5, comment);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Review_Update_ChangesRatingCommentAndKeepsReviewVisible()
    {
        var r = new Review("u1", 1, 7, "Original");
        r.Update(9, "Updated comment");

        r.Rating.Should().Be(9);
        r.Comment.Should().Be("Updated comment");
        r.IsApproved.Should().BeTrue();
    }

    [Fact]
    public void Review_Update_InvalidRating_ThrowsDomainException()
    {
        var r = new Review("u1", 1, 5, "text");
        r.Invoking(x => x.Update(0, "text")).Should().Throw<DomainException>();
    }
}

using System.Security.Claims;
using Cinema.Api.Controllers;
using Cinema.Application.Reviews;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cinema.Tests.Unit.Api;

public sealed class ReviewsControllerUnitTests
{
    [Fact]
    public async Task GetForMovie_ReturnsMovieReviews()
    {
        var reviews = new MovieReviewsDto([Review()], 5, 1);
        var service = new Mock<IReviewService>();
        service.Setup(s => s.GetMovieReviewsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(reviews);
        var controller = CreateController(service.Object);

        var result = await controller.GetForMovie(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(reviews);
    }

    [Fact]
    public async Task GetMine_ReturnsCurrentUserReviews()
    {
        IReadOnlyList<UserReviewDto> reviews = [new(1, 2, "Movie", 5, "Great", DateTime.UtcNow)];
        var service = new Mock<IReviewService>();
        service.Setup(s => s.GetUserReviewsAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(reviews);
        var controller = CreateController(service.Object);

        var result = await controller.GetMine(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(reviews);
    }

    [Fact]
    public async Task CanReview_ReturnsCanReviewAndExistingReview()
    {
        var review = Review();
        var service = new Mock<IReviewService>();
        service.Setup(s => s.CanReviewAsync("user-1", 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        service.Setup(s => s.GetUserReviewAsync("user-1", 2, It.IsAny<CancellationToken>())).ReturnsAsync(review);
        var controller = CreateController(service.Object);

        var result = await controller.CanReview(2, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { canReview = true, myReview = review });
    }

    [Fact]
    public async Task Submit_ReturnsOk_WhenCreated()
    {
        var request = new SubmitReviewRequest(2, 5, "Great");
        var review = Review();
        var service = new Mock<IReviewService>();
        service.Setup(s => s.SubmitAsync("user-1", request, It.IsAny<CancellationToken>())).ReturnsAsync(review);
        var controller = CreateController(service.Object);

        var result = await controller.Submit(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(review);
    }

    [Fact]
    public async Task Submit_ReturnsNotFound_WhenMovieMissing()
    {
        var request = new SubmitReviewRequest(2, 5, "Great");
        var service = new Mock<IReviewService>();
        service.Setup(s => s.SubmitAsync("user-1", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Movie not found."));
        var controller = CreateController(service.Object);

        var result = await controller.Submit(request, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().Be("Movie not found.");
    }

    [Fact]
    public async Task Submit_ReturnsBadRequest_ForBusinessRuleError()
    {
        var request = new SubmitReviewRequest(2, 5, "Great");
        var service = new Mock<IReviewService>();
        service.Setup(s => s.SubmitAsync("user-1", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot review."));
        var controller = CreateController(service.Object);

        var result = await controller.Submit(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>().Subject.Value.Should().Be("Cannot review.");
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenUpdated()
    {
        var request = new UpdateReviewRequest(4, "Updated");
        var review = Review(rating: 4, comment: "Updated");
        var service = new Mock<IReviewService>();
        service.Setup(s => s.UpdateAsync("user-1", 1, request, It.IsAny<CancellationToken>())).ReturnsAsync(review);
        var controller = CreateController(service.Object);

        var result = await controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(review);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenReviewMissing()
    {
        var request = new UpdateReviewRequest(4, "Updated");
        var service = new Mock<IReviewService>();
        service.Setup(s => s.UpdateAsync("user-1", 1, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Review not found."));
        var controller = CreateController(service.Object);

        var result = await controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>().Subject.Value.Should().Be("Review not found.");
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_ForBusinessRuleError()
    {
        var request = new UpdateReviewRequest(4, "Updated");
        var service = new Mock<IReviewService>();
        service.Setup(s => s.UpdateAsync("user-1", 1, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot update."));
        var controller = CreateController(service.Object);

        var result = await controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>().Subject.Value.Should().Be("Cannot update.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_PassesAdminFlagAndReturnsNoContent(bool isAdmin)
    {
        var service = new Mock<IReviewService>();
        var controller = CreateController(service.Object, isAdmin);

        var result = await controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        service.Verify(s => s.DeleteAsync("user-1", 1, isAdmin, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenReviewMissing()
    {
        var service = new Mock<IReviewService>();
        service.Setup(s => s.DeleteAsync("user-1", 1, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Review not found."));
        var controller = CreateController(service.Object);

        var result = await controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>().Subject.Value.Should().Be("Review not found.");
    }

    [Fact]
    public async Task GetAdminReviews_ReturnsAllReviews()
    {
        IReadOnlyList<AdminReviewDto> reviews = [new(1, 2, "User", "Movie", 5, "Great", DateTime.UtcNow)];
        var service = new Mock<IReviewService>();
        service.Setup(s => s.GetAllForAdminAsync(It.IsAny<CancellationToken>())).ReturnsAsync(reviews);
        var controller = CreateController(service.Object, isAdmin: true);

        var result = await controller.GetAdminReviews(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(reviews);
    }

    private static ReviewsController CreateController(IReviewService service, bool isAdmin = false)
    {
        var claims = new List<Claim> { new("sub", "user-1") };
        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        return new ReviewsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role))
                }
            }
        };
    }

    private static ReviewDto Review(int rating = 5, string comment = "Great") =>
        new(1, "user-1", "User", rating, comment, true, DateTime.UtcNow);
}

using System.Security.Claims;
using Cinema.Api.Controllers;
using Cinema.Application.Account;
using Cinema.Application.Tickets;
using Cinema.Domain.Common;
using Cinema.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cinema.Tests.Unit.Api;

public sealed class TicketsControllerUnitTests
{
    [Fact]
    public async Task CreateTickets_ReturnsBadRequest_WhenCashierBuysOnline()
    {
        var controller = CreateController(Mock.Of<ITicketService>(), Mock.Of<IAccountService>(), role: "Cashier");
        var request = Request("buyer@example.com");

        var result = await controller.CreateTickets(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateTickets_ReturnsBadRequest_WhenGuestEmailMissing()
    {
        var tickets = new Mock<ITicketService>();
        var controller = CreateController(tickets.Object, Mock.Of<IAccountService>());

        var result = await controller.CreateTickets(Request(null), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        tickets.Verify(s => s.CreateTicketsAsync(It.IsAny<CreateTicketsRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTickets_ReturnsConflict_WhenSeatsTaken()
    {
        var request = Request("buyer@example.com");
        var tickets = new Mock<ITicketService>();
        tickets.Setup(s => s.CreateTicketsAsync(request, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateTicketsResponse?)null);
        var controller = CreateController(tickets.Object, Mock.Of<IAccountService>());

        var result = await controller.CreateTickets(request, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateTickets_PassesAuthenticatedUserIdAndReturnsOk()
    {
        var request = Request(null);
        var response = new CreateTicketsResponse(
            5,
            120m,
            [new TicketDto(1, new SeatInfo(1, 1, SeatTypeCode.Standard, 120m), "qr", 120m)]);
        var tickets = new Mock<ITicketService>();
        tickets.Setup(s => s.CreateTicketsAsync(request, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var controller = CreateController(tickets.Object, Mock.Of<IAccountService>(), userId: "user-1");

        var result = await controller.CreateTickets(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(response);
    }

    [Theory]
    [InlineData("domain")]
    [InlineData("range")]
    [InlineData("invalid")]
    public async Task CreateTickets_ReturnsBadRequest_ForServiceValidationErrors(string error)
    {
        var request = Request("buyer@example.com");
        var tickets = new Mock<ITicketService>();
        var setup = tickets.Setup(s => s.CreateTicketsAsync(request, null, It.IsAny<CancellationToken>()));
        switch (error)
        {
            case "domain":
                setup.ThrowsAsync(new DomainException("Invalid."));
                break;
            case "range":
                setup.ThrowsAsync(new ArgumentOutOfRangeException("seat"));
                break;
            case "invalid":
                setup.ThrowsAsync(new InvalidOperationException("Invalid."));
                break;
        }
        var controller = CreateController(tickets.Object, Mock.Of<IAccountService>());

        var result = await controller.CreateTickets(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetTicket_ReturnsNotFound_WhenTicketMissingForUser()
    {
        var account = new Mock<IAccountService>();
        account.Setup(s => s.GetTicketDetailAsync(10, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketDetailDto?)null);
        var controller = CreateController(Mock.Of<ITicketService>(), account.Object, userId: "user-1");

        var result = await controller.GetTicket(10, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetTicket_ReturnsOk_WhenTicketFound()
    {
        var detail = TicketDetail();
        var account = new Mock<IAccountService>();
        account.Setup(s => s.GetTicketDetailAsync(10, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);
        var controller = CreateController(Mock.Of<ITicketService>(), account.Object, userId: "user-1");

        var result = await controller.GetTicket(10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(detail);
    }

    [Fact]
    public async Task GetMyTickets_ReturnsCurrentUserTickets()
    {
        IReadOnlyCollection<TicketSummaryDto> summaries =
        [
            new(1, 2, "Movie", DateTime.UtcNow, "Hall", "2D", 1, 1, TicketStatus.Paid, 120m, DateTime.UtcNow)
        ];
        var tickets = new Mock<ITicketService>();
        tickets.Setup(s => s.GetUserTicketsAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries);
        var controller = CreateController(tickets.Object, Mock.Of<IAccountService>(), userId: "user-1");

        var result = await controller.GetMyTickets(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(summaries);
    }

    [Fact]
    public async Task GetQrCode_ReturnsFile_WhenAllowed()
    {
        var stream = new MemoryStream([1, 2, 3]);
        var account = new Mock<IAccountService>();
        account.Setup(s => s.GetTicketQrAsync(10, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);
        var controller = CreateController(Mock.Of<ITicketService>(), account.Object, userId: "user-1");

        var result = await controller.GetQrCode(10, CancellationToken.None);

        var file = result.Should().BeOfType<FileStreamResult>().Subject;
        file.FileStream.Should().BeSameAs(stream);
        file.ContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task GetQrCode_ReturnsNotFound_WhenTicketMissing()
    {
        var account = new Mock<IAccountService>();
        account.Setup(s => s.GetTicketQrAsync(10, "user-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());
        var controller = CreateController(Mock.Of<ITicketService>(), account.Object, userId: "user-1");

        var result = await controller.GetQrCode(10, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetQrCode_ReturnsForbid_WhenUserDoesNotOwnTicket()
    {
        var account = new Mock<IAccountService>();
        account.Setup(s => s.GetTicketQrAsync(10, "user-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController(Mock.Of<ITicketService>(), account.Object, userId: "user-1");

        var result = await controller.GetQrCode(10, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    private static TicketsController CreateController(
        ITicketService ticketService,
        IAccountService accountService,
        string? userId = null,
        string? role = null)
    {
        var controller = new TicketsController(ticketService, accountService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (userId is not null || role is not null)
        {
            List<Claim> claims = [];
            if (userId is not null)
                claims.Add(new Claim("sub", userId));
            if (role is not null)
                claims.Add(new Claim(ClaimTypes.Role, role));

            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));
        }

        return controller;
    }

    private static CreateTicketsRequest Request(string? guestEmail) =>
        new(1, [new SeatCoord(1, 1)], guestEmail, null, null);

    private static TicketDetailDto TicketDetail() =>
        new(
            10,
            1,
            2,
            "Movie",
            DateTime.UtcNow,
            "Hall",
            "2D",
            new SeatInfo(1, 1, SeatTypeCode.Standard, 120m),
            "Paid",
            120m,
            "buyer@example.com",
            "/qr");
}

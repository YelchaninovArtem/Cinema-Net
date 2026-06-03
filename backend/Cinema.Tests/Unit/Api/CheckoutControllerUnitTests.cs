using System.Text;
using Cinema.Api.Controllers;
using Cinema.Application.Cashier;
using Cinema.Application.Payments;
using Cinema.Domain.Common;
using Cinema.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cinema.Tests.Unit.Api;

public sealed class CheckoutControllerUnitTests
{
    [Fact]
    public async Task CashierVerify_ReturnsBadRequest_WhenQrBlank()
    {
        var service = new Mock<ICashierService>();
        var controller = new CashierController(service.Object);

        var result = await controller.Verify(" ", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        service.Verify(s => s.VerifyByQrAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CashierVerify_ReturnsNotFound_WhenTicketMissing()
    {
        var service = new Mock<ICashierService>();
        service.Setup(s => s.VerifyByQrAsync("qr", It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerifyTicketResult?)null);
        var controller = new CashierController(service.Object);

        var result = await controller.Verify("qr", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CashierVerify_ReturnsOk_WhenTicketFound()
    {
        var verified = VerifiedTicket();
        var service = new Mock<ICashierService>();
        service.Setup(s => s.VerifyByQrAsync("qr", It.IsAny<CancellationToken>())).ReturnsAsync(verified);
        var controller = new CashierController(service.Object);

        var result = await controller.Verify("qr", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(verified);
    }

    [Fact]
    public async Task CashierGetTicket_ReturnsNotFound_WhenMissing()
    {
        var service = new Mock<ICashierService>();
        service.Setup(s => s.VerifyByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerifyTicketResult?)null);
        var controller = new CashierController(service.Object);

        var result = await controller.GetTicket(10, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CashierUseTicket_ReturnsConflict_WhenAlreadyUsed()
    {
        var service = new Mock<ICashierService>();
        service.Setup(s => s.UseTicketAsync(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Already used."));
        var controller = new CashierController(service.Object);

        var result = await controller.UseTicket(10, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CashierUseTicket_ReturnsNotFound_WhenMissing()
    {
        var service = new Mock<ICashierService>();
        service.Setup(s => s.UseTicketAsync(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());
        var controller = new CashierController(service.Object);

        var result = await controller.UseTicket(10, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CashierOfflineSale_ReturnsBadRequest_WhenEmailMissing()
    {
        var service = new Mock<ICashierService>();
        var controller = new CashierController(service.Object);
        var request = new OfflineSaleRequest(1, [new SeatCoordRequest(1, 1)], "");

        var result = await controller.OfflineSale(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        service.Verify(s => s.CreateOfflineSaleAsync(It.IsAny<OfflineSaleRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CashierOfflineSale_ReturnsConflict_WhenSeatTaken()
    {
        var service = new Mock<ICashierService>();
        var request = new OfflineSaleRequest(1, [new SeatCoordRequest(1, 1)], "buyer@example.com");
        service.Setup(s => s.CreateOfflineSaleAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfflineSaleResult?)null);
        var controller = new CashierController(service.Object);

        var result = await controller.OfflineSale(request, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CashierOfflineSale_ReturnsBadRequest_WhenDomainException()
    {
        var service = new Mock<ICashierService>();
        var request = new OfflineSaleRequest(1, [new SeatCoordRequest(1, 1)], "buyer@example.com");
        service.Setup(s => s.CreateOfflineSaleAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Invalid seat."));
        var controller = new CashierController(service.Object);

        var result = await controller.OfflineSale(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CashierRefund_ReturnsOk_WhenRefunded()
    {
        var refund = new RefundResult(10, 120m, "Refunded");
        var service = new Mock<ICashierService>();
        service.Setup(s => s.RefundTicketAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        var controller = new CashierController(service.Object);

        var result = await controller.Refund(10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(refund);
    }

    [Fact]
    public async Task PaymentsCreateIntent_ReturnsOk_WithEmptyReturnUrlFallback()
    {
        var response = new CreateIntentResponse("secret", null, "external");
        var service = new Mock<IPaymentService>();
        service.Setup(s => s.CreateIntentAsync(1, "stripe", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var controller = new PaymentsController(service.Object, Mock.Of<IExchangeRateService>());

        var result = await controller.CreateIntent(1, "stripe", new CreateIntentRequest(null), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(response);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("invalid")]
    [InlineData("argument")]
    public async Task PaymentsCreateIntent_MapsServiceErrors(string error)
    {
        var service = new Mock<IPaymentService>();
        var setup = service.Setup(s => s.CreateIntentAsync(1, "stripe", "return", It.IsAny<CancellationToken>()));
        switch (error)
        {
            case "missing":
                setup.ThrowsAsync(new KeyNotFoundException());
                break;
            case "invalid":
                setup.ThrowsAsync(new InvalidOperationException("Invalid."));
                break;
            case "argument":
                setup.ThrowsAsync(new ArgumentException("Bad provider."));
                break;
        }
        var controller = new PaymentsController(service.Object, Mock.Of<IExchangeRateService>());

        var result = await controller.CreateIntent(1, "stripe", new CreateIntentRequest("return"), CancellationToken.None);

        if (error == "missing")
            result.Should().BeOfType<NotFoundResult>();
        else
            result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PaymentsWebhook_ReadsPayloadAndHeaders()
    {
        var service = new Mock<IPaymentService>();
        IReadOnlyDictionary<string, string>? capturedHeaders = null;
        string? capturedPayload = null;
        service.Setup(s => s.HandleWebhookAsync("stripe", It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyDictionary<string, string>, CancellationToken>((_, payload, headers, _) =>
            {
                capturedPayload = payload;
                capturedHeaders = headers;
            })
            .Returns(Task.CompletedTask);
        var controller = new PaymentsController(service.Object, Mock.Of<IExchangeRateService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"ok\":true}"));
        controller.Request.Headers["Stripe-Signature"] = "sig";

        var result = await controller.Webhook("stripe", CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        capturedPayload.Should().Be("{\"ok\":true}");
        capturedHeaders.Should().ContainKey("Stripe-Signature");
    }

    [Fact]
    public async Task PaymentsWebhook_ReturnsBadRequest_WhenProviderInvalid()
    {
        var service = new Mock<IPaymentService>();
        service.Setup(s => s.HandleWebhookAsync("bad", It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Provider."));
        var controller = new PaymentsController(service.Object, Mock.Of<IExchangeRateService>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Body = new MemoryStream();

        var result = await controller.Webhook("bad", CancellationToken.None);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task PaymentsConfirmStripe_ReturnsOk_WhenConfirmed()
    {
        var service = new Mock<IPaymentService>();
        var controller = new PaymentsController(service.Object, Mock.Of<IExchangeRateService>());

        var result = await controller.ConfirmStripe(1, new ConfirmStripeRequest("pi_1"), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        service.Verify(s => s.ConfirmStripeClientAsync(1, "pi_1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PaymentsConfirmGooglePay_ReturnsBadRequest_WhenInvalid()
    {
        var service = new Mock<IPaymentService>();
        service.Setup(s => s.ConfirmWithGooglePayAsync(1, "token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid token."));
        var controller = new PaymentsController(service.Object, Mock.Of<IExchangeRateService>());

        var result = await controller.ConfirmGooglePay(new GooglePayRequest(1, "token"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PaymentsCapturePayPal_ReturnsBadRequest_WhenCaptureFails()
    {
        var service = new Mock<IPaymentService>();
        service.Setup(s => s.CapturePayPalAsync("order", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var controller = new PaymentsController(service.Object, Mock.Of<IExchangeRateService>());

        var result = await controller.CapturePayPal("order", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PaymentsGetExchangeRate_ReturnsUahUsdRate()
    {
        var rate = new ExchangeRateDto("UAH", "USD", 0.025m, DateTime.UtcNow);
        var exchange = new Mock<IExchangeRateService>();
        exchange.Setup(s => s.GetRateAsync("UAH", "USD", It.IsAny<CancellationToken>())).ReturnsAsync(rate);
        var controller = new PaymentsController(Mock.Of<IPaymentService>(), exchange.Object);

        var result = await controller.GetExchangeRate(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(rate);
    }

    private static VerifyTicketResult VerifiedTicket() =>
        new(
            10,
            "Movie",
            "Hall",
            "Branch",
            DateTime.UtcNow,
            "2D",
            1,
            1,
            SeatTypeCode.Standard,
            "Paid",
            "buyer@example.com",
            120m);
}

using Cinema.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _svc;
    private readonly IExchangeRateService _exchangeRate;

    public PaymentsController(IPaymentService svc, IExchangeRateService exchangeRate)
    {
        _svc         = svc;
        _exchangeRate = exchangeRate;
    }

    /// <summary>Створює платіжний intent. Доступно і для гостей, і для зареєстрованих.</summary>
    [HttpPost("{paymentId:int}/intent/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateIntent(
        int paymentId,
        string provider,
        [FromBody] CreateIntentRequest req,
        CancellationToken ct)
    {
        try
        {
            var result = await _svc.CreateIntentAsync(paymentId, provider, req.ReturnUrl ?? "", ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)    { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (ArgumentException ex)         { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Обробляє webhook від Stripe або PayPal. Виклик від платіжної системи, не від клієнта.</summary>
    [HttpPost("webhook/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(string provider, CancellationToken ct)
    {
        try
        {
            // Зчитуємо тіло вручну — Stripe вимагає сирий payload для верифікації підпису
            Request.EnableBuffering();
            var payload = await new System.IO.StreamReader(Request.Body).ReadToEndAsync(ct);
            var headers = Request.Headers.ToDictionary(
                h => h.Key, h => h.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            await _svc.HandleWebhookAsync(provider, payload, headers, ct);
            return Ok();
        }
        catch (ArgumentException) { return BadRequest(); }
    }

    /// <summary>Фіналізує Stripe-платіж після підтвердження на клієнті (коли webhook не налаштований).</summary>
    [HttpPost("{paymentId:int}/confirm-stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmStripe(
        int paymentId,
        [FromBody] ConfirmStripeRequest req,
        CancellationToken ct)
    {
        try
        {
            await _svc.ConfirmStripeClientAsync(paymentId, req.PaymentIntentId, ct);
            return Ok();
        }
        catch (KeyNotFoundException)          { return NotFound(); }
        catch (InvalidOperationException ex)  { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Server-side підтвердження оплати через Google Pay токен.</summary>
    [HttpPost("stripe/google-pay")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmGooglePay(
        [FromBody] GooglePayRequest req, CancellationToken ct)
    {
        try
        {
            await _svc.ConfirmWithGooglePayAsync(req.PaymentId, req.GooglePayToken, ct);
            return Ok();
        }
        catch (KeyNotFoundException)          { return NotFound(); }
        catch (InvalidOperationException ex)  { return BadRequest(new { error = ex.Message }); }
    }

     /// <summary>Захоплення PayPal-ордеру після повернення користувача з PayPal Checkout.</summary>
     [HttpPost("paypal/capture")]
     [AllowAnonymous]
     public async Task<IActionResult> CapturePayPal([FromQuery] string orderId, CancellationToken ct)
     {
         try
         {
             var success = await _svc.CapturePayPalAsync(orderId, ct);
             return success ? Ok() : BadRequest(new { error = "Capture failed." });
         }
         catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
     }

     /// <summary>Отримання поточного курсу обміну UAH→USD.</summary>
     [HttpGet("exchange-rate")]
     [AllowAnonymous]
     public async Task<IActionResult> GetExchangeRate(CancellationToken ct)
     {
         var rate = await _exchangeRate.GetRateAsync("UAH", "USD", ct);
         return Ok(rate);
     }
 }

  public sealed record CreateIntentRequest(string? ReturnUrl);
  public sealed record GooglePayRequest(int PaymentId, string GooglePayToken);
  public sealed record ConfirmStripeRequest(string PaymentIntentId);

using Microsoft.AspNetCore.Mvc;

using Payments.Application.Dtos;
using Payments.Application.Interfaces;

namespace Payments.Api.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _app;

    public PaymentsController(IPaymentService app) => _app = app;

    /// Initiate payment => register.do => return formUrl
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate(PaymentInitiateRequest request, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["X-Idempotency-Key"].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest("X-Idempotency-Key is required.");

        var result = await _app.InitiatePaymentAsync(request, idempotencyKey, ct);
        return Ok(result);
    }

    /// JCC backend does a redirect to this returnUrl with orderId => Verify by calling getOrderStatusExtended.do
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? mdOrder, [FromQuery] string? orderId, CancellationToken ct)
    {
        var gatewayOrderId = !string.IsNullOrWhiteSpace(mdOrder) ? mdOrder : orderId;

        if (string.IsNullOrWhiteSpace(gatewayOrderId))
            return BadRequest("mdOrder or orderId is required.");

        var result = await _app.ConfirmPaymentAsync(gatewayOrderId, ct);        

        var page = "jcc-payments.html";
        // Build redirect URL back to frontend
        var redirectUrl =
            $"http://localhost:5005/" + page +
            $"?status={Uri.EscapeDataString(result.Data?.Status ?? "Error")}" +
            $"&order={Uri.EscapeDataString(result.Data.OrderNumber)}" +
            $"&paymentId={Uri.EscapeDataString(result.Data.PaymentId.ToString())}";

        return Redirect(redirectUrl);

        //return Ok(new
        //{
        //    message = "Callback processed successfully",
        //    gatewayOrderId,
        //    status = result.Status,
        //    result
        //});
    }
}
using Microsoft.AspNetCore.Mvc;

using Payments.Application.Dtos;
using Payments.Application.Interfaces;
using Payments.Application.Services;

namespace Payments.Api.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _app;

    public PaymentsController(IPaymentService app) => _app = app;

    /// Initiate payment => register.do => return formUrl
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate(PaymentInitiateRequestDto request, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["X-Idempotency-Key"].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest("X-Idempotency-Key is required.");

        var result = await _app.InitiateAsync(request, idempotencyKey, ct);
        return Ok(result);
    }

    /// JCC backend does a redirect to this returnUrl with orderId => Verify by calling getOrderStatusExtended.do
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? mdOrder, [FromQuery] string? orderId, CancellationToken ct)
    {
        var gatewayOrderId = !string.IsNullOrWhiteSpace(mdOrder) ? mdOrder : orderId;

        if (string.IsNullOrWhiteSpace(gatewayOrderId))
            return BadRequest("mdOrder or orderId is required.");

        var result = await _app.ConfirmByGatewayOrderIdAsync(gatewayOrderId, ct);

        //// Return a small HTML that:
        //// 1) postMessage
        //// 2) closes popup
        //var html = $$"""
        //   <html>
        //     <body>
        //       <script>
        //         const payload = {{System.Text.Json.JsonSerializer.Serialize(result)}};
        //         if (window.opener && !window.opener.closed) {
        //             window.opener.postMessage(
        //               { type: "JCC_PAYMENT_RESULT", payload: payload },
        //               "*"
        //             );
        //         }
        //         window.close();
        //       </script>
        //     </body>
        //   </html>
        //   """;

        //return Content(html, "text/html");

        var page = "jcc-multiframe.html";
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
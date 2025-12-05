using Microsoft.AspNetCore.Mvc;

using Payments.Application.Dtos;
using Payments.Application.Services;

namespace Payments.Api.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _app;

    public PaymentsController(PaymentService app) => _app = app;

    /// Initiate payment => register.do => return formUrl
    [HttpPost("initiate")]
    public async Task<ActionResult<PaymentInitiateResponseDto>> Initiate(PaymentInitiateRequestDto req, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["X-Idempotency-Key"].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest("X-Idempotency-Key is required.");

        var result = await _app.InitiateAsync(req, idempotencyKey, ct);
        return Ok(result);
    }

    /// JCC redirects user to returnUrl with orderId Verify by calling getOrderStatusExtended.do
    [HttpGet("callback")]
    public async Task<ActionResult<PaymentResultDto>> Callback([FromQuery] string? mdOrder, [FromQuery] string? orderId, CancellationToken ct)
    {
        var gatewayOrderId = !string.IsNullOrWhiteSpace(mdOrder) ? mdOrder : orderId;

        if (string.IsNullOrWhiteSpace(gatewayOrderId))
            return BadRequest("mdOrder or orderId is required.");

        var result = await _app.ConfirmByGatewayOrderIdAsync(gatewayOrderId, ct);

        // Return a small HTML that:
        // 1) postMessage
        // 2) closes popup
        var html = $$"""
           <html>
             <body>
               <script>
                 const payload = {{System.Text.Json.JsonSerializer.Serialize(result)}};
                 if (window.opener && !window.opener.closed) {
                     window.opener.postMessage(
                       { type: "JCC_PAYMENT_RESULT", payload: payload },
                       "*"
                     );
                 }
                 window.close();
               </script>
             </body>
           </html>
           """;

        return Content(html, "text/html");

        //var page = "banktransfer2.html";
        //// Build redirect URL back to frontend
        //var redirectUrl =
        //    $"http://localhost:5005/" + page + 
        //    $"?status={Uri.EscapeDataString(result.Status)}" +
        //    $"&order={Uri.EscapeDataString(result.OrderNumber)}" +
        //    $"&paymentId={Uri.EscapeDataString(result.PaymentId.ToString())}";

        //return Redirect(redirectUrl);

        //return Ok(new
        //{
        //    message = "Callback processed successfully",
        //    gatewayOrderId,
        //    status = result.Status,
        //    result
        //});
    }
}
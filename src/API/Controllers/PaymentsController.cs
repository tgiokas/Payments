using Microsoft.AspNetCore.Mvc;
using Payments.Application.Dtos;
using Payments.Application.Services;

namespace DMS.Payment.WebAPI.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _app;

    public PaymentsController(PaymentService app) => _app = app;

    // 1) Initiate payment => register.do => return formUrl
    [HttpPost("initiate")]
    public async Task<ActionResult<PaymentInitiateResponseDto>> Initiate(
        PaymentInitiateRequestDto req,
        CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["X-Idempotency-Key"].ToString();
        var tenantKey = Request.Headers["X-Tenant-Key"].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest("X-Idempotency-Key is required.");

        var result = await _app.InitiateAsync(req, idempotencyKey, tenantKey, ct);
        return Ok(result);
    }

    // 2) ReturnUrl landing from JCC includes orderId :contentReference[oaicite:11]{index=11}
    // We verify by calling getOrderStatusExtended.do
    [HttpGet("callback")]
    public async Task<ActionResult<PaymentResultDto>> Callback(
        [FromQuery] string? mdOrder,
        [FromQuery] string? orderId,
       CancellationToken ct)
    {
        var gatewayOrderId = !string.IsNullOrWhiteSpace(mdOrder) ? mdOrder : orderId;

        if (string.IsNullOrWhiteSpace(gatewayOrderId))
            return BadRequest("mdOrder or orderId is required.");

        var result = await _app.ConfirmByGatewayOrderIdAsync(orderId, ct);

        // Return a small HTML that:
        // 1) postMessage to opener
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
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var p = await _app.GetAsync(id, ct);
        return p is null ? NotFound() : Ok(p);
    }
}

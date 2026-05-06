using Microsoft.AspNetCore.Mvc;

using Payments.Application.Dtos;
using Payments.Application.Interfaces;

namespace Payments.Api.Controllers;

[ApiController]   
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _config;
    private readonly string? _defaultFrontEndUrl;

    public PaymentsController(IPaymentService paymentService, IConfiguration config)
    {
        _paymentService = paymentService;
        _config = config;

        _defaultFrontEndUrl = _config["JCC_FRONTEND_RETURN_URL"];
    }

    /// Initiate payment => register.do => return formUrl
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate(PaymentInitiateRequest request, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["X-Idempotency-Key"].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest("X-Idempotency-Key is required.");

        var result = await _paymentService.InitiatePaymentAsync(request, idempotencyKey, ct);
        return Ok(result);
    }

    /// JCC backend does a redirect to this returnUrl with orderId => Verify by calling getOrderStatusExtended.do
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? mdOrder, [FromQuery] string? orderId, CancellationToken ct)
    {
        var gatewayOrderId = !string.IsNullOrWhiteSpace(mdOrder) ? mdOrder : orderId;

        if (string.IsNullOrWhiteSpace(gatewayOrderId))
            return BadRequest("mdOrder or orderId is required.");

        var result = await _paymentService.ConfirmPaymentAsync(gatewayOrderId, ct);

        // Resolve frontend return URL: base URL + applicationId
        var applicationId = await _paymentService.GetApplicationIdByGatewayOrderIdAsync(gatewayOrderId, ct);

        var baseUrl = _defaultFrontEndUrl;

        if (string.IsNullOrWhiteSpace(baseUrl))
            return BadRequest("No frontend base URL configured.");
        
        var frontendUrl = !string.IsNullOrWhiteSpace(applicationId)
            ? $"{baseUrl.TrimEnd('/')}/{applicationId}"
            : baseUrl;

        // Build redirect URL back to frontend
        var redirectUrl =
            $"{frontendUrl}" +
            $"?status={Uri.EscapeDataString(result.Data?.Status ?? "Error")}" +
            $"&order={Uri.EscapeDataString(result.Data?.OrderNumber!)}" +
            $"&paymentId={Uri.EscapeDataString(result.Data?.PaymentId.ToString()!)}";

        return Redirect(redirectUrl);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        return Ok(await _paymentService.GetByIdAsync(id));
    }

    [HttpGet("order/{orderNumber}")]
    public async Task<IActionResult> GetPaymentByOrder(string orderNumber)
    {
        return Ok(await _paymentService.GetByOrderNumberAsync(orderNumber));
    }
}

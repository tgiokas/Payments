using Microsoft.AspNetCore.Mvc;
using GlobalPaymentsHpp.Services;

namespace GlobalPaymentsHpp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, IConfiguration config, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Creates payment request data for HPP redirect
    /// </summary>
    [HttpPost("create")]
    public ActionResult<PaymentRequestResponse> CreatePayment([FromBody] PaymentInput input)
    {
        try
        {
            var request = _paymentService.CreatePaymentRequest(input);
            var hppUrl = _config["GlobalPayments:HppUrl"] ?? "https://pay.sandbox.realexpayments.com/pay";

            _logger.LogInformation("Payment request created: OrderId={OrderId}, Amount={Amount}",
                request.OrderId, request.Amount);

            return Ok(new PaymentRequestResponse
            {
                HppUrl = hppUrl,
                FormData = request,
                RedirectHtml = GenerateAutoSubmitForm(hppUrl, request)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment request");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Receives response from Global Payments HPP (callback endpoint)
    /// </summary>
    [HttpPost("response")]
    public IActionResult HandleResponse([FromForm] PaymentResponse response)
    {
        _logger.LogInformation("Payment response received: OrderId={OrderId}, Result={Result}",
            response.OrderId, response.Result);

        var isValid = _paymentService.ValidateResponse(response);

        if (!isValid)
        {
            _logger.LogWarning("Invalid hash in payment response for OrderId={OrderId}", response.OrderId);
            return Content(GenerateRedirectScript($"/payment-result?status=error&message=Invalid%20hash&orderId={response.OrderId}"),
                "text/html");
        }

        var status = response.Result == "00" ? "success" : "failed";
        var redirectUrl = $"/payment-result?status={status}&orderId={response.OrderId}&message={Uri.EscapeDataString(response.Message)}";

        // TODO: Save transaction to database here
        // await _transactionRepository.SaveAsync(new Transaction { ... });

        // Global Payments HPP displays the response content, so we return JS redirect
        return Content(GenerateRedirectScript(redirectUrl), "text/html");
    }

    /// <summary>
    /// Alternative GET endpoint for testing response handling
    /// </summary>
    [HttpGet("response")]
    public IActionResult HandleResponseGet(
        [FromQuery] string? orderId, [FromQuery] string? result,
        [FromQuery] string? message, [FromQuery] string? timestamp,
        [FromQuery] string? pasRef, [FromQuery] string? authCode)
    {
        return Ok(new
        {
            orderId,
            result,
            message,
            timestamp,
            pasRef,
            authCode,
            status = result == "00" ? "success" : "failed"
        });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });

    private static string GenerateAutoSubmitForm(string url, PaymentRequest req)
    {
        return $@"<!DOCTYPE html>
<html><head><title>Redirecting to payment...</title></head>
<body onload=""document.getElementById('payform').submit();"">
<form id=""payform"" method=""POST"" action=""{url}"">
    <input type=""hidden"" name=""MERCHANT_ID"" value=""{req.MerchantId}""/>
    <input type=""hidden"" name=""ACCOUNT"" value=""{req.Account}""/>
    <input type=""hidden"" name=""ORDER_ID"" value=""{req.OrderId}""/>
    <input type=""hidden"" name=""AMOUNT"" value=""{req.Amount}""/>
    <input type=""hidden"" name=""CURRENCY"" value=""{req.Currency}""/>
    <input type=""hidden"" name=""TIMESTAMP"" value=""{req.Timestamp}""/>
    <input type=""hidden"" name=""SHA1HASH"" value=""{req.Sha1Hash}""/>
    <input type=""hidden"" name=""AUTO_SETTLE_FLAG"" value=""{req.AutoSettleFlag}""/>
    <input type=""hidden"" name=""HPP_VERSION"" value=""{req.HppVersion}""/>
    <input type=""hidden"" name=""HPP_CHANNEL"" value=""{req.HppChannel}""/>
    <input type=""hidden"" name=""MERCHANT_RESPONSE_URL"" value=""{req.MerchantResponseUrl}""/>
    <input type=""hidden"" name=""HPP_BILLING_STREET1"" value=""{req.HppBillingStreet1}""/>
    <input type=""hidden"" name=""HPP_BILLING_CITY"" value=""{req.HppBillingCity}""/>
    <input type=""hidden"" name=""HPP_BILLING_POSTALCODE"" value=""{req.HppBillingPostalCode}""/>
    <input type=""hidden"" name=""HPP_BILLING_COUNTRY"" value=""{req.HppBillingCountry}""/>
    <input type=""hidden"" name=""HPP_CUSTOMER_EMAIL"" value=""{req.HppCustomerEmail}""/>
    <input type=""hidden"" name=""COMMENT1"" value=""{req.Comment1}""/>
    <input type=""hidden"" name=""HPP_LANG"" value=""{req.HppLang}""/>
    <noscript><input type=""submit"" value=""Click here to continue""/></noscript>
</form>
<p>Redirecting to secure payment page...</p>
</body></html>";
    }

    private static string GenerateRedirectScript(string url) =>
        $"<script>window.top.location.replace('{url}');</script>";
}

public record PaymentRequestResponse
{
    public string HppUrl { get; init; } = "";
    public PaymentRequest FormData { get; init; } = new();
    public string RedirectHtml { get; init; } = "";
}
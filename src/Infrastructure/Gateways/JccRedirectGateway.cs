using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payments.Application.Dtos;
using Payments.Domain.Entities;
using Payments.Infrastructure.Security;

namespace Payments.Infrastructure.Gateways;

public class JccRedirectGateway : IJccRedirectGateway
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<JccRedirectGateway> _logger;
    private readonly IJccRequestSigner? _signer; // optional

    public JccRedirectGateway(HttpClient http, IConfiguration config, ILogger<JccRedirectGateway> logger, IJccRequestSigner? signer = null)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _signer = signer;
    }

    public async Task<RegisterOrderResult> RegisterOrderAsync(JccRegisterOrderRequestDto request, CancellationToken ct = default)
    {
        var baseUrl = _config["Jcc:RestBaseUrl"];
        var url = $"{baseUrl}/register.do";
        var minorAmount = ToMinorUnits(request.Amount);
        var currencyNumeric = _config["Jcc:CurrencyNumeric"] ?? "978";
        var returnUrl = _config["Jcc:ReturnUrl"];
        var language = _config["Jcc:Language"] ?? "en";
        var description = $"Order {request.OrderNumber}";

        var form = new Dictionary<string, string>
        {
            ["amount"] = minorAmount.ToString(CultureInfo.InvariantCulture),
            ["currency"] = currencyNumeric,
            ["returnUrl"] = returnUrl!,
            ["failUrl"] = returnUrl!,
            ["orderNumber"] = request.OrderNumber,
            ["description"] = description,
            ["language"] = language
        };

        ApplyAuth(form);

        var body = BuildFormBody(form);
        var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

        _logger.LogInformation("Calling JCC register.do for {OrderNumber}", request.OrderNumber);

        if (_signer != null)
        {
            var (xHash, xSig) = _signer.Sign(body);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            _http.DefaultRequestHeaders.Remove("X-Hash");
            _http.DefaultRequestHeaders.Remove("X-Signature");
            _http.DefaultRequestHeaders.Add("X-Hash", xHash);
            _http.DefaultRequestHeaders.Add("X-Signature", xSig);
        }

        var resp = await _http.PostAsync(url, content, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return new RegisterOrderResult(false, null, null, "HTTP_" + resp.StatusCode, json);

        var dto = JsonSerializer.Deserialize<JccRegisterOrderResponseDto>(json);

        string? gatewayOrderId = dto?.OrderId;
        string? formUrl = dto?.FormUrl;

        if (!string.IsNullOrWhiteSpace(formUrl))
        {
            var md = ExtractQueryParam(formUrl, "mdOrder");
            if (!string.IsNullOrWhiteSpace(md))
                gatewayOrderId = md;
        }

        if (!string.IsNullOrWhiteSpace(gatewayOrderId) && !string.IsNullOrWhiteSpace(formUrl))
            return new RegisterOrderResult(true, gatewayOrderId, formUrl, null, null);

        return new RegisterOrderResult(false, null, null, dto?.ErrorCode ?? "UNKNOWN", dto?.ErrorMessage ?? json);
    }

    public async Task<OrderStatusResult> GetOrderStatusExtendedAsync(string gatewayOrderId, CancellationToken ct = default)
    {
        var baseUrl = _config["Jcc:RestBaseUrl"];
        var url = $"{baseUrl}/getOrderStatusExtended.do";

        var form = new Dictionary<string, string>
        {
            ["orderId"] = gatewayOrderId
        };

        ApplyAuth(form);

        var body = BuildFormBody(form);
        var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

        if (_signer != null)
        {
            var (xHash, xSig) = _signer.Sign(body);
            _http.DefaultRequestHeaders.Remove("X-Hash");
            _http.DefaultRequestHeaders.Remove("X-Signature");
            _http.DefaultRequestHeaders.Add("X-Hash", xHash);
            _http.DefaultRequestHeaders.Add("X-Signature", xSig);
        }

        _logger.LogInformation("Calling JCC getOrderStatusExtended.do for {GatewayOrderId}", gatewayOrderId);

        var resp = await _http.PostAsync(url, content, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return new OrderStatusResult(false, null, null, "HTTP_" + resp.StatusCode, json);

        var dto = JsonSerializer.Deserialize<JccOrderStatusResponseDto>(json);

        return new OrderStatusResult(
            true,
            dto?.OrderStatus,
            dto?.ActionCode,
            dto?.ErrorCode,
            dto?.ErrorMessage
        );
    }

    private void ApplyAuth(Dictionary<string, string> form)
    {
        var token = _config["Jcc:Token"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            form["token"] = token;
            return;
        }

        form["userName"] = _config["Jcc:UserName"]!;
        form["password"] = _config["Jcc:Password"]!;
    }

    private static string? ExtractQueryParam(string url, string key)
    {
        var uri = new Uri(url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query[key];
    }

    private static long ToMinorUnits(decimal amount)
        => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    private static string BuildFormBody(Dictionary<string, string> form)
        => string.Join("&", form.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
}

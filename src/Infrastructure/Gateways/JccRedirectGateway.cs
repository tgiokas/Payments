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

    public async Task<RegisterOrderResult> RegisterOrderAsync(Payment payment, CancellationToken ct = default)
    {
        // register.do parameters :contentReference[oaicite:6]{index=6}
        var baseUrl = _config["Jcc:RestBaseUrl"]; // e.g. https://gateway-test.jcc.com.cy/payment/rest
        var url = $"{baseUrl}/register.do";

        // amount in minor currency units (e.g., 20.00 EUR => 2000) :contentReference[oaicite:7]{index=7}
        var minorAmount = ToMinorUnits(payment.AmountValue);

        var currencyNumeric = _config["Jcc:CurrencyNumeric"] ?? "978"; // EUR=978

        var returnUrl = _config["Jcc:ReturnUrl"]; // your API callback/return landing page
        var language = _config["Jcc:Language"] ?? "en";
        var description = $"Order {payment.OrderNumber}";

        var form = new Dictionary<string, string>
        {
            ["amount"] = minorAmount.ToString(CultureInfo.InvariantCulture),
            ["currency"] = currencyNumeric,
            ["returnUrl"] = returnUrl!,
            ["failUrl"] = returnUrl!,
            ["orderNumber"] = payment.OrderNumber,
            ["description"] = description,
            ["language"] = language
        };

        ApplyAuth(form);

        // IMPORTANT: build the same body that FormUrlEncodedContent will send
        var body = BuildFormBody(form);

        var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

        _logger.LogInformation("Calling JCC register.do for {OrderNumber}", payment.OrderNumber);

        // If signatures are enabled for your merchant, add headers
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

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // REST Redirect returns "orderId"; MultiECom returns "orderId" too
        // but the formUrl contains mdOrder. We support both.
        string? gatewayOrderId =
            root.TryGetProperty("orderId", out var oid) ? oid.GetString() : null;

        string? formUrl =
            root.TryGetProperty("formUrl", out var fu) ? fu.GetString() : null;

        if (!string.IsNullOrWhiteSpace(formUrl))
        {
            // If mdOrder is present in URL, prefer it (MultiECom case)
            var md = ExtractQueryParam(formUrl, "mdOrder");
            if (!string.IsNullOrWhiteSpace(md))
                gatewayOrderId = md;
        }

        if (!string.IsNullOrWhiteSpace(gatewayOrderId) && !string.IsNullOrWhiteSpace(formUrl))
            return new RegisterOrderResult(true, gatewayOrderId, formUrl, null, null);

        var errorCode = root.TryGetProperty("errorCode", out var ec) ? ec.GetString() : "UNKNOWN";
        var errorMessage = root.TryGetProperty("errorMessage", out var em) ? em.GetString() : json;

        return new RegisterOrderResult(false, null, null, errorCode, errorMessage);
    }

    public async Task<OrderStatusResult> GetOrderStatusExtendedAsync(string gatewayOrderId, CancellationToken ct = default)
    {
        // getOrderStatusExtended.do call per redirect flow :contentReference[oaicite:9]{index=9}
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

        using var doc = JsonDocument.Parse(json);

        var orderStatus = doc.RootElement.TryGetProperty("orderStatus", out var os)
            ? os.GetInt32()
            : (int?)null;

        var actionCode = doc.RootElement.TryGetProperty("actionCode", out var ac)
            ? ac.GetString()
            : null;

        // success response may omit error fields
        var errorCode = doc.RootElement.TryGetProperty("errorCode", out var ec) ? ec.GetString() : null;
        var errorMessage = doc.RootElement.TryGetProperty("errorMessage", out var em) ? em.GetString() : null;

        return new OrderStatusResult(true, orderStatus, actionCode, errorCode, errorMessage);
    }

    // Auth can be userName/password OR token :contentReference[oaicite:10]{index=10}
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

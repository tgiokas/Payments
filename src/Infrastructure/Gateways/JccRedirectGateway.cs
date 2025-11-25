using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Payments.Application.Dtos;
using Payments.Infrastructure.ApiClients;
using Payments.Infrastructure.Security;

namespace Payments.Infrastructure.Gateways;

public class JccRedirectGateway : ApiClientBase, IJccRedirectGateway
{
    private readonly IConfiguration _config;
    private readonly IJccRequestSigner? _signer;

    private readonly string _baseUrl;
    private readonly string _currencyNumeric;
    private readonly string _defaultReturnUrl;
    private readonly string _defaultLanguage;

    public JccRedirectGateway(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<JccRedirectGateway> logger,
        IJccRequestSigner? signer = null)
        : base(httpClient, logger)
    {
        _config = config;
        _signer = signer;

        _baseUrl = _config["Jcc:RestBaseUrl"] ?? throw new ArgumentNullException("Jcc:RestBaseUrl is missing.");
        _currencyNumeric = _config["Jcc:CurrencyNumeric"] ?? "978";        
        _defaultReturnUrl = _config["Jcc:ReturnUrl"] ?? throw new ArgumentNullException("Jcc:ReturnUrl is missing.");
        _defaultLanguage = _config["Jcc:Language"] ?? "en";
    }
    
    public async Task<RegisterOrderResult> RegisterOrderAsync(JccRegisterOrderRequestDto req, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/register.do";

        var minorAmount = ToMinorUnits(req.Amount);
        var language = req.Language ?? _defaultLanguage;
        var returnUrl = req.ReturnUrl ?? _defaultReturnUrl;
        var description = req.Description ?? $"Order {req.OrderNumber}";

        var form = new Dictionary<string, string>
        {
            ["amount"] = minorAmount.ToString(CultureInfo.InvariantCulture),
            ["currency"] = _currencyNumeric,
            ["returnUrl"] = returnUrl,
            ["failUrl"] = returnUrl,
            ["orderNumber"] = req.OrderNumber,
            ["description"] = description,
            ["language"] = language
        };

        ApplyAuth(form);

        var body = BuildFormBody(form);
        var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        if (_signer != null)
        {
            var (xHash, xSig) = _signer.Sign(body);
            request.Headers.TryAddWithoutValidation("X-Hash", xHash);
            request.Headers.TryAddWithoutValidation("X-Signature", xSig);
        }

        _logger.LogInformation("Calling JCC register.do for {OrderNumber}", req.OrderNumber);

        var resp = await SendRequestRetryAsync(request, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return new RegisterOrderResult(false, null, null, "HTTP_" + resp.StatusCode, json);

        var registerOrderDto = JsonSerializer.Deserialize<JccRegisterOrderResponseDto>(json);

        string? gatewayOrderId = registerOrderDto?.OrderId;
        string? formUrl = registerOrderDto?.FormUrl;

        if (!string.IsNullOrWhiteSpace(formUrl))
        {
            var md = ExtractQueryParam(formUrl, "mdOrder");
            if (!string.IsNullOrWhiteSpace(md))
                gatewayOrderId = md;
        }

        if (!string.IsNullOrWhiteSpace(gatewayOrderId) && !string.IsNullOrWhiteSpace(formUrl))
            return new RegisterOrderResult(true, gatewayOrderId, formUrl, null, null);

        return new RegisterOrderResult(false, null, null, registerOrderDto?.ErrorCode ?? "UNKNOWN", registerOrderDto?.ErrorMessage ?? json);
    }

    public async Task<OrderStatusResult> GetOrderStatusExtendedAsync(string gatewayOrderId, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/getOrderStatusExtended.do";

        var form = new Dictionary<string, string>
        {
            ["orderId"] = gatewayOrderId
        };

        ApplyAuth(form);

        var body = BuildFormBody(form);
        var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        if (_signer != null)
        {
            var (xHash, xSig) = _signer.Sign(body);
            request.Headers.TryAddWithoutValidation("X-Hash", xHash);
            request.Headers.TryAddWithoutValidation("X-Signature", xSig);
        }

        _logger.LogInformation("Calling JCC getOrderStatusExtended.do for {GatewayOrderId}", gatewayOrderId);

        var resp = await SendRequestRetryAsync(request, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return new OrderStatusResult(false, null, null, "HTTP_" + resp.StatusCode, json);

        var orderStatusDto = JsonSerializer.Deserialize<JccOrderStatusResponseDto>(json);

        return new OrderStatusResult(
            true,
            orderStatusDto?.OrderStatus,
            orderStatusDto?.ActionCode,
            orderStatusDto?.ErrorCode,
            orderStatusDto?.ErrorMessage
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

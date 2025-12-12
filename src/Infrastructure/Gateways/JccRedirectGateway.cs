using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
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

    public async Task<RegisterOrderResultDto> RegisterOrderAsync(JccRegisterOrderRequest req, CancellationToken ct = default)
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
            ["orderNumber"] = req.OrderNumber,
            ["description"] = description,
            ["language"] = language,
            ["returnUrl"] = returnUrl,
            ["failUrl"] = returnUrl
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

        var resp = await SendRequestAsync(request, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return new RegisterOrderResultDto
            {
                Success = false,
                GatewayOrderId = null,
                FormUrl = null,
                ErrorCode = resp.StatusCode.ToString(),
                ErrorMessage = json
            };       

        var registerOrderDto = JsonSerializer.Deserialize<JccRegisterOrderResponse>(json);

        string? gatewayOrderId = registerOrderDto?.OrderId;
        string? formUrl = registerOrderDto?.FormUrl;

        if (!string.IsNullOrWhiteSpace(formUrl))
        {
            var md = ExtractQueryParam(formUrl, "mdOrder");
            if (!string.IsNullOrWhiteSpace(md))
                gatewayOrderId = md;
        }

        if (!string.IsNullOrWhiteSpace(gatewayOrderId) && !string.IsNullOrWhiteSpace(formUrl))
            return new RegisterOrderResultDto
            {
                Success = true,
                GatewayOrderId = gatewayOrderId,
                FormUrl = formUrl,
                ErrorCode = null,
                ErrorMessage = null
            };

        return new RegisterOrderResultDto
        {
            Success = false,
            GatewayOrderId = null,
            FormUrl = null,
            ErrorCode = registerOrderDto?.ErrorCode ?? "UNKNOWN",
            ErrorMessage = registerOrderDto?.ErrorMessage ?? json
        };
    }

    public async Task<OrderStatusResultDto> GetOrderStatusAsync(string gatewayOrderId, CancellationToken ct = default)
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

        var resp = await SendRequestAsync(request, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return new OrderStatusResultDto
            {
                Success = false,
                OrderStatus = null,
                ActionCode = null,
                ErrorCode = "HTTP_" + resp.StatusCode,
                ErrorMessage = json
            };

        var orderStatusDto = JsonSerializer.Deserialize<JccOrderStatusResponse>(json);

        return new OrderStatusResultDto
        {
            Success = true,
            OrderStatus = orderStatusDto?.OrderStatus,
            ActionCode = orderStatusDto?.ActionCode,
            ErrorCode = orderStatusDto?.ErrorCode,
            ErrorMessage = orderStatusDto?.ErrorMessage
        };
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
        var query = HttpUtility.ParseQueryString(uri.Query);
        return query[key];
    }

    private static long ToMinorUnits(decimal amount)
        => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    private static string BuildFormBody(Dictionary<string, string> form)
        => string.Join("&", form.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
}
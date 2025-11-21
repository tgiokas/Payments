using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace GlobalPaymentsHpp.Services;

public interface IPaymentService
{
    PaymentRequest CreatePaymentRequest(PaymentInput input);
    bool ValidateResponse(PaymentResponse response);
    string GenerateRequestHash(string timestamp, string merchantId, string orderId, string amount, string currency);
    string GenerateResponseHash(string timestamp, string merchantId, string orderId, string result, string message, string pasRef, string authCode);
}

public class PaymentService : IPaymentService
{
    private readonly PaymentSettings _settings;

    public PaymentService(IConfiguration config)
    {
        _settings = config.GetSection("GlobalPayments").Get<PaymentSettings>()
            ?? throw new InvalidOperationException("GlobalPayments settings not configured");
    }

    public PaymentRequest CreatePaymentRequest(PaymentInput input)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var orderId = input.OrderId ?? GenerateOrderId();
        var amountInCents = ((int)(input.Amount * 100)).ToString();

        var hash = GenerateRequestHash(timestamp, _settings.MerchantId, orderId, amountInCents, input.Currency);

        return new PaymentRequest
        {
            MerchantId = _settings.MerchantId,
            Account = _settings.Account,
            OrderId = orderId,
            Amount = amountInCents,
            Currency = input.Currency,
            Timestamp = timestamp,
            Sha1Hash = hash,
            AutoSettleFlag = "1",
            HppVersion = "2",
            HppChannel = "ECOM",
            MerchantResponseUrl = input.ResponseUrl ?? _settings.ResponseUrl,
            // Billing info
            HppBillingStreet1 = input.BillingStreet1,
            HppBillingCity = input.BillingCity,
            HppBillingPostalCode = input.BillingPostalCode,
            HppBillingCountry = input.BillingCountryCode,
            HppCustomerEmail = input.CustomerEmail,
            // Optional fields
            Comment1 = input.Comment ?? "Payment via .NET 8 Microservice",
            HppLang = input.Language ?? "en"
        };
    }

    public bool ValidateResponse(PaymentResponse response)
    {
        var expectedHash = GenerateResponseHash(
            response.Timestamp, response.MerchantId, response.OrderId,
            response.Result, response.Message, response.PasRef, response.AuthCode);
        return string.Equals(expectedHash, response.Sha1Hash, StringComparison.OrdinalIgnoreCase);
    }

    public string GenerateRequestHash(string timestamp, string merchantId, string orderId, string amount, string currency)
    {
        // Step 1: Hash TIMESTAMP.MERCHANTID.ORDERID.AMOUNT.CURRENCY
        var step1 = $"{timestamp}.{merchantId}.{orderId}.{amount}.{currency}";
        var hash1 = ComputeSha1(step1);
        // Step 2: Hash hash1.secret
        var step2 = $"{hash1}.{_settings.SharedSecret}";
        return ComputeSha1(step2);
    }

    public string GenerateResponseHash(string timestamp, string merchantId, string orderId, string result, string message, string pasRef, string authCode)
    {
        // Step 1: Hash TIMESTAMP.MERCHANTID.ORDERID.RESULT.MESSAGE.PASREF.AUTHCODE
        var step1 = $"{timestamp}.{merchantId}.{orderId}.{result}.{message}.{pasRef}.{authCode}";
        var hash1 = ComputeSha1(step1);
        // Step 2: Hash hash1.secret
        var step2 = $"{hash1}.{_settings.SharedSecret}";
        return ComputeSha1(step2);
    }

    private static string ComputeSha1(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA1.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GenerateOrderId() =>
        Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_").Replace("+", "-")[..22];
}

// Models
public record PaymentSettings
{
    public string MerchantId { get; init; } = "";
    public string Account { get; init; } = "internet";
    public string SharedSecret { get; init; } = "";
    public string ResponseUrl { get; init; } = "";
    public string HppUrl { get; init; } = "https://pay.sandbox.realexpayments.com/pay";
}

public record PaymentInput
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string? OrderId { get; init; }
    public string? ResponseUrl { get; init; }
    public string? BillingStreet1 { get; init; }
    public string? BillingCity { get; init; }
    public string? BillingPostalCode { get; init; }
    public string? BillingCountryCode { get; init; }
    public string? CustomerEmail { get; init; }
    public string? Comment { get; init; }
    public string? Language { get; init; }
}

public record PaymentRequest
{
    public string MerchantId { get; init; } = "";
    public string Account { get; init; } = "";
    public string OrderId { get; init; } = "";
    public string Amount { get; init; } = "";
    public string Currency { get; init; } = "";
    public string Timestamp { get; init; } = "";
    public string Sha1Hash { get; init; } = "";
    public string AutoSettleFlag { get; init; } = "1";
    public string HppVersion { get; init; } = "2";
    public string HppChannel { get; init; } = "ECOM";
    public string MerchantResponseUrl { get; init; } = "";
    public string? HppBillingStreet1 { get; init; }
    public string? HppBillingCity { get; init; }
    public string? HppBillingPostalCode { get; init; }
    public string? HppBillingCountry { get; init; }
    public string? HppCustomerEmail { get; init; }
    public string? Comment1 { get; init; }
    public string? HppLang { get; init; }
}

public record PaymentResponse
{
    public string Timestamp { get; init; } = "";
    public string MerchantId { get; init; } = "";
    public string OrderId { get; init; } = "";
    public string Result { get; init; } = "";
    public string Message { get; init; } = "";
    public string PasRef { get; init; } = "";
    public string AuthCode { get; init; } = "";
    public string Sha1Hash { get; init; } = "";
    public string? Cvnresult { get; init; }
    public string? Avspostcoderesult { get; init; }
    public string? Avsaddressresult { get; init; }
    public string? Batchid { get; init; }
}
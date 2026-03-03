using System.Text.Json.Serialization;

namespace Payments.Application.Dtos;

public class JccOrderStatusResponse
{
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("orderStatus")]
    public int? OrderStatus { get; set; }

    [JsonPropertyName("actionCode")]
    public int? ActionCode { get; set; }

    [JsonPropertyName("actionCodeDescription")]
    public string? ActionCodeDescription { get; set; }

    [JsonPropertyName("displayErrorMessage")]
    public string? DisplayErrorMessage { get; set; }

    // Note: JCC uses minor units (e.g. 2000 = 20.00)
    [JsonPropertyName("amount")]
    public long? Amount { get; set; }

    // Numeric currency (e.g. "978")
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    // Unix epoch millis in JCC response
    [JsonPropertyName("date")]
    public long? Date { get; set; }

    [JsonPropertyName("depositedDate")]
    public long? DepositedDate { get; set; }

    [JsonPropertyName("ip")]
    public string? Ip { get; set; }

    [JsonPropertyName("cardAuthInfo")]
    public JccCardAuthInfo? CardAuthInfo { get; set; }

    [JsonPropertyName("authDateTime")]
    public long? AuthDateTime { get; set; }

    [JsonPropertyName("terminalId")]
    public string? TerminalId { get; set; }

    [JsonPropertyName("authRefNum")]
    public string? AuthRefNum { get; set; }

    [JsonPropertyName("paymentAmountInfo")]
    public JccPaymentAmountInfo? PaymentAmountInfo { get; set; }

    [JsonPropertyName("bankInfo")]
    public JccBankInfo? BankInfo { get; set; }

    [JsonPropertyName("payerData")]
    public JccPayerData? PayerData { get; set; }

    [JsonPropertyName("paymentWay")]
    public string? PaymentWay { get; set; }
}

public class JccCardAuthInfo
{
    [JsonPropertyName("maskedPan")]
    public string? MaskedPan { get; set; }

    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    [JsonPropertyName("cardholderName")]
    public string? CardholderName { get; set; }

    [JsonPropertyName("approvalCode")]
    public string? ApprovalCode { get; set; }

    [JsonPropertyName("paymentSystem")]
    public string? PaymentSystem { get; set; }
}

public class JccPaymentAmountInfo
{
    [JsonPropertyName("paymentState")]
    public string? PaymentState { get; set; }

    [JsonPropertyName("approvedAmount")]
    public long? ApprovedAmount { get; set; }

    [JsonPropertyName("depositedAmount")]
    public long? DepositedAmount { get; set; }

    [JsonPropertyName("refundedAmount")]
    public long? RefundedAmount { get; set; }

    [JsonPropertyName("feeAmount")]
    public long? FeeAmount { get; set; }

    [JsonPropertyName("totalAmount")]
    public long? TotalAmount { get; set; }
}

public class JccBankInfo
{
    [JsonPropertyName("bankName")]
    public string? BankName { get; set; }

    [JsonPropertyName("bankCountryCode")]
    public string? BankCountryCode { get; set; }

    [JsonPropertyName("bankCountryName")]
    public string? BankCountryName { get; set; }
}

public class JccPayerData
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
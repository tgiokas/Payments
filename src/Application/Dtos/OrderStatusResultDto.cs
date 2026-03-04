namespace Payments.Application.Dtos;

public class OrderStatusResultDto
{
    public bool Success { get; set; }
    public int? OrderStatus { get; set; }
    public int? ActionCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    // Bank Info
    public string? BankName { get; set; }
    public string? BankCountryCode { get; set; }
    public string? BankCountryName { get; set; }

    // Card Info
    public string? MaskedPan { get; set; }
    public string? Expiration { get; set; }
    public string? CardholderName { get; set; }
    public string? ApprovalCode { get; set; }
    public string? PaymentSystem { get; set; }

    // Payment Amount Info
    public string? PaymentState { get; set; }
    public long? ApprovedAmount { get; set; }
    public long? DepositedAmount { get; set; }
    public long? RefundedAmount { get; set; }
    public long? FeeAmount { get; set; }
    public long? TotalAmount { get; set; }

    public string? PaymentWay { get; set; }
    public string? Email { get; set; }
}
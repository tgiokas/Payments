namespace Payments.Application.Dtos;

public class PaymentDto
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? GatewayOrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

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
    public decimal? ApprovedAmount { get; set; }
    public decimal? DepositedAmount { get; set; }
    public decimal? RefundedAmount { get; set; }
    public decimal? FeeAmount { get; set; }
    public decimal? TotalAmount { get; set; }

    public string? PaymentWay { get; set; }
    public string? ActionCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
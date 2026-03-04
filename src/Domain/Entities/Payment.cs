using Payments.Domain.Enums;

namespace Payments.Domain.Entities;

public class Payment
{   
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Description { get; set; }
    // Payment order reference (unique)
    public string OrderNumber { get; set; } = string.Empty;
    // JCC order id (returned by register.do)
    public string? GatewayOrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string IdempotencyKey { get; set; } = string.Empty;

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
    public JccOrderStatus OrderStatus { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}
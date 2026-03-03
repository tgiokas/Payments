using Payments.Domain.Enums;
using System.Text.Json.Serialization;

namespace Payments.Domain.Entities;

public class Payment
{   
    public Guid Id { get; set; } = Guid.NewGuid();
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

    // PaymentAmount Info
    public string? PaymentState { get; set; }
    public long? ApprovedAmount { get; set; }  
    public long? DepositedAmount { get; set; }   
    public long? RefundedAmount { get; set; }    
    public long? FeeAmount { get; set; } 
    public long? TotalAmount { get; set; }

    public string? PaymentWay { get; set; }
    public string? ActionCode { get; set; }
    public JccOrderStatus OrderStatus { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}
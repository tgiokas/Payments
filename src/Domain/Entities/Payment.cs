using Payments.Domain.Enums;

namespace Payments.Domain.Entities;

public class Payment
{   
    public Guid Id { get; set; } = Guid.NewGuid();
    // Payment order reference (unique)
    public string OrderNumber { get; set; } = string.Empty;
    // JCC order id (returned by register.do)
    public string? GatewayOrderId { get; set; }
    public decimal AmountValue { get; set; }
    public string AmountCurrency { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? JccActionCode { get; set; }
    public JccOrderStatus JccOrderStatus { get; set; }
    public string? JccErrorCode { get; set; }
    public string? JccErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}
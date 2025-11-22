using Payments.Domain.Enums;
using Payments.Domain.ValueObjects;

namespace Payments.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Your merchant-side order reference (unique)
    public string OrderNumber { get; set; } = default!;

    // JCC order id (returned by register.do)
    public string? GatewayOrderId { get; set; }

    public decimal AmountValue { get; set; }
    public string AmountCurrency { get; set; } = default!;
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string IdempotencyKey { get; set; } = default!;
    public string? TenantKey { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
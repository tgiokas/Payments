using Payments.Domain.Enums;

namespace Payments.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Your merchant-side order reference (unique)
    public string OrderNumber { get; set; } = default!;

    // JCC order id (returned by register.do)
    public string? GatewayOrderId { get; set; }

    //public Money Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string IdempotencyKey { get; set; } = default!;
    public string? TenantKey { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
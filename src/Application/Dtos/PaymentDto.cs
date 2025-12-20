namespace Payments.Application.Dtos;

public class PaymentDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? GatewayOrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string Method { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
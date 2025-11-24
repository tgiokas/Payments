namespace Payments.Application.Dtos;

public class PaymentInitiateResponseDto
{
    public Guid PaymentId { get; set; }
    public string GatewayOrderId { get; set; } = default!;
    public string FormUrl { get; set; } = default!;
    public string Status { get; set; } = default!;
}
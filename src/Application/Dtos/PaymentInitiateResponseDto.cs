namespace Payments.Application.Dtos;

public class PaymentInitiateResponseDto
{
    public Guid PaymentId { get; set; }
    public string GatewayOrderId { get; set; } = string.Empty;
    public string FormUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
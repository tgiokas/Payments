namespace Payments.Application.Dtos;

public class PaymentInitiateRequestDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}
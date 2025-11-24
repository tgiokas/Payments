namespace Payments.Application.Dtos;

public class PaymentInitiateRequestDto
{
    public string OrderNumber { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string Method { get; set; } = default!;
}
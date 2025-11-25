namespace Payments.Application.Dtos;

public class JccRegisterOrderRequestDto
{
    public string OrderNumber { get; set; } = default!;
    public decimal Amount { get; set; }             // major units
    public string Currency { get; set; } = "EUR";   // major units ISO code
    public string? Description { get; set; }
    public string? Language { get; set; }
    public string? ReturnUrl { get; set; }
}

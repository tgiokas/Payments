namespace Payments.Application.Dtos;

public class JccRegisterOrderRequest
{
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? Description { get; set; }
    public string? Language { get; set; }
    public string? ReturnUrl { get; set; }
}
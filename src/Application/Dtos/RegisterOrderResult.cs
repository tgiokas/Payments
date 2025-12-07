namespace Payments.Application.Dtos;

public class RegisterOrderResult
{
    public bool Success { get; set; }
    public string? GatewayOrderId { get; set; }
    public string? FormUrl { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
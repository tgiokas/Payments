namespace Payments.Application.Dtos;

public class PaymentConfirmResponse
{
    public Guid PaymentId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string GatewayOrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;      // Approved / Declined / Error
    public string? ActionCode { get; set; }                 // optional from JCC status response
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
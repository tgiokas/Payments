namespace Payments.Application.Dtos;

public class OrderStatusResultDto
{
    public bool Success { get; set; }
    public int? OrderStatus { get; set; }
    public int? ActionCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }    
}
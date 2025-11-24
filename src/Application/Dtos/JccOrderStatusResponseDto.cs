namespace Payments.Application.Dtos;

public class JccOrderStatusResponseDto
{
    public int? OrderStatus { get; set; }
    public int? ActionCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

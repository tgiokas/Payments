namespace Payments.Application.Dtos;

public record RegisterOrderResult(
    bool Success,
    string? GatewayOrderId,
    string? FormUrl,
    string? ErrorCode,
    string? ErrorMessage
);

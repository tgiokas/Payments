namespace Payments.Application.Dtos;

public record PaymentResultDto(
    Guid PaymentId,
    string OrderNumber,
    string GatewayOrderId,
    string Status,       // Approved / Declined / Error
    string? ActionCode,  // optional from JCC status response
    string? ErrorCode,
    string? ErrorMessage
);
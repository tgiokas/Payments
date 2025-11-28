namespace Payments.Application.Dtos;

public record RegisterOrderResult(
    bool Success,
    string? GatewayOrderId,
    string? FormUrl,
    string? BankAccount,
    string? PayCode,
    string? ErrorCode,
    string? ErrorMessage
);

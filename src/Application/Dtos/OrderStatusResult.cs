namespace Payments.Application.Dtos;

public record OrderStatusResult(
    bool Success,
    int? OrderStatus,     // 2 success per JCC docs
    int? ActionCode,
    string? ErrorCode,
    string? ErrorMessage
);

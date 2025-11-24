namespace Payments.Application.Dtos;

public record PaymentInitiateResponseDto(
    Guid PaymentId,
    string GatewayOrderId,
    string FormUrl,
    string Status // "Redirected"
);
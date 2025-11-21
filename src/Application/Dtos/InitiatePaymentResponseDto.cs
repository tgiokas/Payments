namespace Payments.Application.Dtos;

public record InitiatePaymentResponseDto(
    Guid PaymentId,
    string GatewayOrderId,
    string FormUrl,
    string Status // "Redirected"
);